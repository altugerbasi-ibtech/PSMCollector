using Microsoft.Data.SqlClient;

namespace Inventory.Web.Services;

public sealed class TestConnectionPoolService(IConfiguration configuration) : IAsyncDisposable
{
    public const int TargetConnectionCount = 50;
    public const int ActiveTestConnectionCount = 40;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<SqlConnection> _connections = [];
    private readonly List<Task> _activeTasks = [];
    private CancellationTokenSource? _activeCancellation;
    private readonly string _connectionString = configuration.GetConnectionString("TestConnectionsDatabase")
        ?? throw new InvalidOperationException("TestConnectionsDatabase connection string is required.");

    public int OpenConnectionCount => _connections.Count(x => x.State == System.Data.ConnectionState.Open);
    public int ActiveWorkloadCount => _activeTasks.Count(x => !x.IsCompleted);
    public bool IsRunning => OpenConnectionCount > 0;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (IsRunning) return;
            await CloseCoreAsync();
            var pending = Enumerable.Range(0, TargetConnectionCount)
                .Select(_ => new SqlConnection(_connectionString))
                .ToArray();
            try
            {
                await Task.WhenAll(pending.Select(x => x.OpenAsync(cancellationToken)));
                _connections.AddRange(pending);
            }
            catch
            {
                await Task.WhenAll(pending.Select(x => x.DisposeAsync().AsTask()));
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try { await CloseCoreAsync(); }
        finally { _gate.Release(); }
    }

    public async Task StartActiveWorkloadAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) await StartAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (ActiveWorkloadCount > 0) return;
            await StopActiveWorkloadCoreAsync();
            _activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            for (var index = 0; index < Math.Min(ActiveTestConnectionCount, _connections.Count - 1); index++)
                _activeTasks.Add(RunActiveQueryLoopAsync(_connections[index], _activeCancellation.Token));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopActiveWorkloadAsync()
    {
        await _gate.WaitAsync();
        try { await StopActiveWorkloadCoreAsync(); }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<IisInventoryRunRow>> ReadRunsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var ownsConnection = !IsRunning;
            var connection = ownsConnection ? new SqlConnection(_connectionString) : _connections[^1];
            try
            {
                if (ownsConnection) await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT TOP (100) RunId, IisServer, StartedAt, CompletedAt, CollectorVersion, RunStatus, ErrorMessage
                    FROM dbo.IisInventoryRun
                    ORDER BY StartedAt DESC;
                    """;
                var rows = new List<IisInventoryRunRow>();
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add(new IisInventoryRunRow(
                        reader.GetGuid(0), reader.GetString(1), reader.GetDateTime(2), reader.GetDateTime(3),
                        reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
                }
                return rows;
            }
            finally
            {
                if (ownsConnection) await connection.DisposeAsync();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task CloseCoreAsync()
    {
        await StopActiveWorkloadCoreAsync();
        foreach (var connection in _connections) await connection.DisposeAsync();
        _connections.Clear();
        SqlConnection.ClearAllPools();
    }

    private async Task StopActiveWorkloadCoreAsync()
    {
        if (_activeCancellation is null) return;
        await _activeCancellation.CancelAsync();
        try { await Task.WhenAll(_activeTasks); }
        catch (Exception ex) when (ex is OperationCanceledException or SqlException) { }
        _activeTasks.Clear();
        _activeCancellation.Dispose();
        _activeCancellation = null;
    }

    private static async Task RunActiveQueryLoopAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandText = "WAITFOR DELAY '00:05:00';";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _gate.Dispose();
    }
}

public sealed record IisInventoryRunRow(
    Guid RunId, string IisServer, DateTime StartedAt, DateTime CompletedAt,
    string CollectorVersion, string RunStatus, string? ErrorMessage);
