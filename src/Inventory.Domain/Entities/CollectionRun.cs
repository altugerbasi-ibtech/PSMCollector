using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public sealed class CollectionRun
{
    public Guid Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public CollectionRunStatus Status { get; set; }
    public int IisServerCount { get; set; }
    public int SuccessfulIisServerCount { get; set; }
    public int FailedIisServerCount { get; set; }
    public int StagedConnectionCount { get; set; }
    public int MatchedConnectionCount { get; set; }
    public int UnmatchedConnectionCount { get; set; }
    public string? ErrorSummary { get; set; }
}
