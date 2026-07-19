namespace Inventory.Domain.Entities;

public abstract class AuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public required string UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
