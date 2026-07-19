using System.Data;
using Inventory.Application.Services;
using Inventory.Contracts;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inventory.SqlCollector;

public sealed class SqlCollectorOptions
{
    public int PollIntervalSeconds { get; set; } = 2;
    public int HeartbeatIntervalSeconds { get; set; } = 15;
    public int MaxSnapshotAgeSeconds { get; set; } = 120;
}

public sealed partial class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<SqlCollectorOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly SqlCollectorOptions _options = options.Value;
    private DateTime _lastHeartbeatUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, Environment.UserName);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if ((DateTime.UtcNow - _lastHeartbeatUtc).TotalSeconds >= _options.HeartbeatIntervalSeconds)
                    await WriteHeartbeatAsync("Running", stoppingToken);
                await ProcessNextRunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                LogCycleFailed(logger, ex);
                await WriteHeartbeatAsync($"Error: {Normalize(ex.Message)}", stoppingToken);
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
        }
    }

    private async Task ProcessNextRunAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.CollectionRuns
            .Where(x => x.Status == CollectionRunStatus.ProcessingSql && x.CompletedAtUtc == null && x.StartedAtUtc < DateTime.UtcNow.AddSeconds(-30))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, CollectionRunStatus.ReadyForSql), cancellationToken);
        var run = await db.CollectionRuns
            .Where(x => x.Status == CollectionRunStatus.ReadyForSql || x.Status == CollectionRunStatus.Partial)
            .OrderBy(x => x.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (run is null) return;

        run.Status = CollectionRunStatus.ProcessingSql;
        await db.SaveChangesAsync(cancellationToken);
        var staging = await db.IisConnectionStaging.Where(x => x.CollectionId == run.Id).ToListAsync(cancellationToken);
        if (staging.Count == 0)
        {
            run.Status = CollectionRunStatus.Completed;
            run.CompletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }
        if ((DateTime.UtcNow - staging.Min(x => x.CollectedAtUtc)).TotalSeconds > _options.MaxSnapshotAgeSeconds)
        {
            run.Status = CollectionRunStatus.Expired;
            run.CompletedAtUtc = DateTime.UtcNow;
            run.ErrorSummary = "IIS socket snapshot exceeded configured maximum age.";
            staging.ForEach(x => x.ProcessingStatus = StagingProcessingStatus.Expired);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var errors = new List<string>();
        var inventory = new List<ConnectionInventory>();
        foreach (var endpointGroup in staging.GroupBy(x => x.SqlServerIp))
        {
            var sqlServer = await db.SqlServers.SingleOrDefaultAsync(x => x.IsEnabled && x.IpAddress == endpointGroup.Key, cancellationToken);
            if (sqlServer is null)
            {
                await UpsertUnknownAsync(db, endpointGroup.ToList(), cancellationToken);
                run.UnmatchedConnectionCount += endpointGroup.Count();
                continue;
            }

            try
            {
                var sessions = await ReadSessionsAsync(sqlServer, endpointGroup.ToList(), cancellationToken);
                var lookup = sessions.Select(x => (x.ClientIp, x.ClientPort)).ToHashSet();
                run.MatchedConnectionCount += endpointGroup.Count(x => lookup.Contains((x.ClientIp, x.ClientPort)));
                run.UnmatchedConnectionCount += endpointGroup.Count(x => !lookup.Contains((x.ClientIp, x.ClientPort)));
                foreach (var iisGroup in endpointGroup.GroupBy(x => x.IisServerId))
                {
                    var iis = await db.IisServers.SingleAsync(x => x.Id == iisGroup.Key, cancellationToken);
                    var observations = iisGroup.Select(x => new IisConnectionObservation(x.CollectedAtUtc, x.IisServerName, x.AppPoolName, x.WorkerProcessId, x.ClientIp, x.ClientPort, x.SqlServerIp, x.SqlServerPort));
                    inventory.AddRange(ConnectionInventoryAggregator.Aggregate(run.Id, DateTime.UtcNow, iis, sqlServer, observations, sessions));
                }
                sqlServer.LastConnectionStatus = CollectionStatus.Success;
                sqlServer.LastErrorMessage = null;
            }
            catch (Exception ex)
            {
                var message = Normalize(ex.Message);
                sqlServer.LastConnectionStatus = CollectionStatus.Failed;
                sqlServer.LastErrorMessage = message;
                endpointGroup.ToList().ForEach(x => { x.ProcessingStatus = StagingProcessingStatus.Failed; x.LastErrorMessage = message; x.RetryCount++; });
                errors.Add($"{sqlServer.ServerName}: {message}");
            }
        }

        await db.ConnectionInventory.Where(x => x.CollectionId == run.Id).ExecuteDeleteAsync(cancellationToken);
        db.ConnectionInventory.AddRange(inventory);
        db.IisConnectionStaging.RemoveRange(staging.Where(x => x.ProcessingStatus != StagingProcessingStatus.Failed));
        run.Status = errors.Count == 0 ? CollectionRunStatus.Completed : CollectionRunStatus.Failed;
        run.CompletedAtUtc = DateTime.UtcNow;
        run.ErrorSummary = errors.Count == 0 ? run.ErrorSummary : Normalize(string.Join(" | ", errors));
        await db.SaveChangesAsync(cancellationToken);
        LogRunCompleted(logger, run.Id, inventory.Count);
    }

    private static async Task<List<SqlSessionSnapshot>> ReadSessionsAsync(SqlServer server, List<IisConnectionStaging> staging, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{server.Fqdn},{server.Port}", InitialCatalog = "master", IntegratedSecurity = true,
            Encrypt = true, TrustServerCertificate = server.TrustServerCertificate,
            ConnectTimeout = server.ConnectionTimeoutSeconds, ApplicationName = "PSMCollector.DatabaseCollector"
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE #IisConnections (ClientIp varchar(48) NOT NULL, ClientPort int NOT NULL);";
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
        var table = new DataTable();
        table.Columns.Add("ClientIp", typeof(string)); table.Columns.Add("ClientPort", typeof(int));
        foreach (var item in staging.Select(x => new { x.ClientIp, x.ClientPort }).Distinct()) table.Rows.Add(item.ClientIp, item.ClientPort);
        using (var bulk = new SqlBulkCopy(connection))
        {
            bulk.DestinationTableName = "#IisConnections";
            bulk.ColumnMappings.Add("ClientIp", "ClientIp"); bulk.ColumnMappings.Add("ClientPort", "ClientPort");
            await bulk.WriteToServerAsync(table, cancellationToken);
        }
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT c.client_net_address, c.client_tcp_port, c.session_id,
       COALESCE(DB_NAME(r.database_id), DB_NAME(s.database_id), N'unknown') AS DatabaseName,
       CONVERT(bit, CASE WHEN r.session_id IS NULL THEN 0 ELSE 1 END) AS HasActiveRequest
FROM sys.dm_exec_connections c
JOIN #IisConnections i ON i.ClientIp = c.client_net_address AND i.ClientPort = c.client_tcp_port
JOIN sys.dm_exec_sessions s ON s.session_id = c.session_id
LEFT JOIN sys.dm_exec_requests r ON r.session_id = c.session_id
WHERE c.local_tcp_port = 1433;";
        var result = new List<SqlSessionSnapshot>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new SqlSessionSnapshot(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3), reader.GetBoolean(4)));
        return result;
    }

    private static async Task UpsertUnknownAsync(InventoryDbContext db, List<IisConnectionStaging> rows, CancellationToken cancellationToken)
    {
        var ip = rows[0].SqlServerIp;
        var endpoint = await db.UnknownSqlEndpoints.SingleOrDefaultAsync(x => x.IpAddress == ip && x.Port == 1433, cancellationToken);
        if (endpoint is null)
        {
            endpoint = new UnknownSqlEndpoint { IpAddress = ip, Port = 1433, FirstSeenUtc = DateTime.UtcNow, LastSeenUtc = DateTime.UtcNow, LastIisServerName = rows[0].IisServerName };
            db.UnknownSqlEndpoints.Add(endpoint);
        }
        endpoint.LastSeenUtc = DateTime.UtcNow; endpoint.ObservationCount += rows.Count; endpoint.LastIisServerName = rows[0].IisServerName;
    }

    private async Task WriteHeartbeatAsync(string status, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var instance = Environment.MachineName;
        var heartbeat = await db.CollectorHeartbeats.SingleOrDefaultAsync(x => x.CollectorType == CollectorType.Sql && x.InstanceName == instance, cancellationToken);
        if (heartbeat is null)
        {
            heartbeat = new CollectorHeartbeat { CollectorType = CollectorType.Sql, InstanceName = instance, Version = typeof(Worker).Assembly.GetName().Version?.ToString() ?? "1.0", LastHeartbeatUtc = DateTime.UtcNow };
            db.CollectorHeartbeats.Add(heartbeat);
        }
        heartbeat.LastHeartbeatUtc = DateTime.UtcNow; heartbeat.StatusMessage = status;
        await db.SaveChangesAsync(cancellationToken); _lastHeartbeatUtc = DateTime.UtcNow;
    }

    private static string Normalize(string value)
    {
        var normalized = value.ReplaceLineEndings(" ").Trim();
        return normalized[..Math.Min(2000, normalized.Length)];
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database collector started as {Identity}")]
    private static partial void LogStarted(ILogger logger, string identity);
    [LoggerMessage(Level = LogLevel.Error, Message = "Database collection cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
    [LoggerMessage(Level = LogLevel.Information, Message = "Run {RunId} completed with {Rows} inventory rows")]
    private static partial void LogRunCompleted(ILogger logger, Guid runId, int rows);
}
