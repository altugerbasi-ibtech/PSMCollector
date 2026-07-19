using System.Diagnostics;
using System.Text.Json;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inventory.IisCollector;

public sealed class IisCollectorOptions
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int CollectionIntervalSeconds { get; set; } = 60;
    public int HeartbeatIntervalSeconds { get; set; } = 15;
    public string PowerShellPath { get; set; } = "powershell.exe";
}

public sealed partial class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<IisCollectorOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IisCollectorOptions _options = options.Value;
    private DateTime _lastHeartbeatUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, Environment.UserName, _options.CollectionIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                LogCycleFailed(logger, ex);
                await WriteHeartbeatAsync($"Error: {Normalize(ex.Message)}", stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastHeartbeatUtc).TotalSeconds >= _options.HeartbeatIntervalSeconds)
            await WriteHeartbeatAsync("Running", cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var due = await db.IisServers.Where(x => x.IsEnabled &&
            (x.LastCollectionAttemptUtc == null ||
             EF.Functions.DateDiffSecond(x.LastCollectionAttemptUtc, now) >= _options.CollectionIntervalSeconds))
            .ToListAsync(cancellationToken);
        if (due.Count == 0) return;

        var run = new CollectionRun
        {
            Id = Guid.NewGuid(), StartedAtUtc = now, Status = CollectionRunStatus.PendingIis,
            IisServerCount = due.Count
        };
        db.CollectionRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var errors = new List<string>();
        foreach (var server in due)
        {
            server.LastCollectionAttemptUtc = DateTime.UtcNow;
            server.LastCollectionStatus = CollectionStatus.Running;
            server.LastErrorMessage = null;
            await db.SaveChangesAsync(cancellationToken);
            try
            {
                var observations = await CollectAsync(server, cancellationToken);
                foreach (var item in observations.DistinctBy(x => new { x.AppPoolName, x.WorkerProcessId, x.ClientIp, x.ClientPort, x.SqlServerIp }))
                {
                    db.IisConnectionStaging.Add(new IisConnectionStaging
                    {
                        CollectionId = run.Id, CollectedAtUtc = DateTime.UtcNow,
                        IisServerId = server.Id, IisServerName = server.ServerName,
                        AppPoolName = item.AppPoolName, WorkerProcessId = item.WorkerProcessId,
                        ClientIp = item.ClientIp, ClientPort = item.ClientPort,
                        SqlServerIp = item.SqlServerIp, SqlServerPort = 1433,
                        ProcessingStatus = StagingProcessingStatus.Pending
                    });
                }
                server.LastSuccessfulCollectionUtc = DateTime.UtcNow;
                server.LastCollectionStatus = CollectionStatus.Success;
                run.SuccessfulIisServerCount++;
                run.StagedConnectionCount += observations.Count;
                LogCollected(logger, observations.Count, server.ServerName);
            }
            catch (Exception ex)
            {
                var message = Normalize(ex.Message);
                server.LastCollectionStatus = CollectionStatus.Failed;
                server.LastErrorMessage = message;
                run.FailedIisServerCount++;
                errors.Add($"{server.ServerName}: {message}");
                LogServerFailed(logger, ex, server.ServerName);
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        run.Status = run.SuccessfulIisServerCount > 0
            ? (run.FailedIisServerCount > 0 ? CollectionRunStatus.Partial : CollectionRunStatus.ReadyForSql)
            : CollectionRunStatus.Failed;
        if (run.Status == CollectionRunStatus.Failed) run.CompletedAtUtc = DateTime.UtcNow;
        run.ErrorSummary = errors.Count == 0 ? null : Normalize(string.Join(" | ", errors));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<SocketObservation>> CollectAsync(IisServer server, CancellationToken cancellationToken)
    {
        var target = string.IsNullOrWhiteSpace(server.Fqdn) ? server.ServerName : server.Fqdn;
        var inner = @"
Import-Module WebAdministration
$result = foreach ($pool in Get-ChildItem IIS:\AppPools) {
  foreach ($worker in $pool.workerProcesses.Collection) {
    Get-NetTCPConnection -OwningProcess $worker.processId -State Established -RemotePort 1433 -ErrorAction SilentlyContinue | ForEach-Object {
      [pscustomobject]@{ AppPoolName=$pool.Name; WorkerProcessId=[int]$worker.processId; ClientIp=$_.LocalAddress; ClientPort=[int]$_.LocalPort; SqlServerIp=$_.RemoteAddress }
    }
  }
}
ConvertTo-Json -InputObject @($result) -Compress
";
        var isLocal = target.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                      target.StartsWith($"{Environment.MachineName}.", StringComparison.OrdinalIgnoreCase) ||
                      target.Equals("localhost", StringComparison.OrdinalIgnoreCase) || target == ".";
        var script = isLocal
            ? inner
            : $"Invoke-Command -ComputerName '{target.Replace("'", "''", StringComparison.Ordinal)}' -ScriptBlock {{ {inner} }}";
        var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
        var start = new ProcessStartInfo(_options.PowerShellPath, $"-NoLogo -NoProfile -NonInteractive -EncodedCommand {encoded}")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell could not be started.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(server.ConnectionTimeoutSeconds));
        await process.WaitForExitAsync(timeout.Token);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0) throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? $"PowerShell exited with {process.ExitCode}." : stderr);
        if (string.IsNullOrWhiteSpace(stdout)) return [];
        return JsonSerializer.Deserialize<List<SocketObservation>>(stdout, SerializerOptions) ?? [];
    }

    private async Task WriteHeartbeatAsync(string status, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var instance = Environment.MachineName;
        var heartbeat = await db.CollectorHeartbeats.SingleOrDefaultAsync(x => x.CollectorType == CollectorType.Iis && x.InstanceName == instance, cancellationToken);
        if (heartbeat is null)
        {
            heartbeat = new CollectorHeartbeat { CollectorType = CollectorType.Iis, InstanceName = instance, Version = typeof(Worker).Assembly.GetName().Version?.ToString() ?? "1.0", LastHeartbeatUtc = DateTime.UtcNow };
            db.CollectorHeartbeats.Add(heartbeat);
        }
        heartbeat.LastHeartbeatUtc = DateTime.UtcNow;
        heartbeat.StatusMessage = status;
        await db.SaveChangesAsync(cancellationToken);
        _lastHeartbeatUtc = DateTime.UtcNow;
    }

    private static string Normalize(string value) => value.ReplaceLineEndings(" ").Trim()[..Math.Min(2000, value.ReplaceLineEndings(" ").Trim().Length)];
    private sealed record SocketObservation(string AppPoolName, int WorkerProcessId, string ClientIp, int ClientPort, string SqlServerIp);

    [LoggerMessage(Level = LogLevel.Information, Message = "IIS collector started as {Identity}; interval {Interval}s")]
    private static partial void LogStarted(ILogger logger, string identity, int interval);
    [LoggerMessage(Level = LogLevel.Error, Message = "IIS collection cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
    [LoggerMessage(Level = LogLevel.Information, Message = "Collected {Count} SQL sockets from {Server}")]
    private static partial void LogCollected(ILogger logger, int count, string server);
    [LoggerMessage(Level = LogLevel.Error, Message = "Collection failed for {Server}")]
    private static partial void LogServerFailed(ILogger logger, Exception exception, string server);
}
