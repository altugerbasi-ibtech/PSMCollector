using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public sealed class CollectorHeartbeat
{
    public int Id { get; set; }
    public CollectorType CollectorType { get; set; }
    public required string InstanceName { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public required string Version { get; set; }
    public string? StatusMessage { get; set; }
}
