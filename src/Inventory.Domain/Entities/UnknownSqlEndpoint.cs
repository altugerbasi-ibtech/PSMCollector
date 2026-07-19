namespace Inventory.Domain.Entities;

public sealed class UnknownSqlEndpoint
{
    public int Id { get; set; }
    public required string IpAddress { get; set; }
    public int Port { get; set; } = 1433;
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public long ObservationCount { get; set; }
    public required string LastIisServerName { get; set; }
}
