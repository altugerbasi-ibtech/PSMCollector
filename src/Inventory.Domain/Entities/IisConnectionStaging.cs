using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public sealed class IisConnectionStaging
{
    public long Id { get; set; }
    public Guid CollectionId { get; set; }
    public CollectionRun CollectionRun { get; set; } = null!;
    public DateTime CollectedAtUtc { get; set; }
    public int IisServerId { get; set; }
    public IisServer IisServer { get; set; } = null!;
    public required string IisServerName { get; set; }
    public required string AppPoolName { get; set; }
    public int WorkerProcessId { get; set; }
    public required string ClientIp { get; set; }
    public int ClientPort { get; set; }
    public required string SqlServerIp { get; set; }
    public int SqlServerPort { get; set; } = 1433;
    public StagingProcessingStatus ProcessingStatus { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastErrorMessage { get; set; }
}
