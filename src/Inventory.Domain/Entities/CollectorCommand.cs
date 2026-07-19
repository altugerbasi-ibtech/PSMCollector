using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public sealed class CollectorCommand
{
    public Guid Id { get; set; }
    public CollectorType CollectorType { get; set; }
    public CollectorCommandType CommandType { get; set; }
    public int TargetId { get; set; }
    public CollectorCommandStatus Status { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public required string RequestedBy { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResultMessage { get; set; }
}
