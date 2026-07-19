namespace Inventory.Domain.Entities;

public sealed class ConnectionInventory
{
    public long Id { get; set; }
    public Guid CollectionId { get; set; }
    public CollectionRun CollectionRun { get; set; } = null!;
    public DateTime InventoryDateUtc { get; set; }
    public int IisServerId { get; set; }
    public IisServer IisServer { get; set; } = null!;
    public required string IisServerName { get; set; }
    public required string AppPoolName { get; set; }
    public int SqlServerId { get; set; }
    public SqlServer SqlServer { get; set; } = null!;
    public required string SqlServerName { get; set; }
    public string? SqlInstance { get; set; }
    public required string DatabaseName { get; set; }
    public required string SqlEndpoint { get; set; }
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdlePooledConnections { get; set; }
    public string? WorkerProcessIds { get; set; }

    public bool HasValidTotals => TotalConnections == ActiveConnections + IdlePooledConnections;
}
