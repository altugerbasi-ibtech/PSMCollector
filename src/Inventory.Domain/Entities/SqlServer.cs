using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public sealed class SqlServer : AuditableEntity
{
    public int Id { get; set; }
    public required string ServerName { get; set; }
    public required string Fqdn { get; set; }
    public required string IpAddress { get; set; }
    public int Port { get; set; } = 1433;
    public bool IsEnabled { get; set; } = true;
    public bool TrustServerCertificate { get; set; }
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public CollectionStatus? LastConnectionStatus { get; set; }
    public string? LastErrorMessage { get; set; }
}
