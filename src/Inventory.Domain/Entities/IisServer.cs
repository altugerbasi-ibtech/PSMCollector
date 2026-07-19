using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public sealed class IisServer : AuditableEntity
{
    public int Id { get; set; }
    public required string ServerName { get; set; }
    public string? Fqdn { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int CollectionIntervalSeconds { get; set; } = 60;
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public string? Description { get; set; }
    public DateTime? LastCollectionAttemptUtc { get; set; }
    public DateTime? LastSuccessfulCollectionUtc { get; set; }
    public CollectionStatus? LastCollectionStatus { get; set; }
    public string? LastErrorMessage { get; set; }

    public bool IsDue(DateTime utcNow) =>
        IsEnabled && (LastCollectionAttemptUtc is null ||
                      LastCollectionAttemptUtc.Value.AddSeconds(CollectionIntervalSeconds) <= utcNow);
}
