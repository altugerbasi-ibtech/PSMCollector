namespace Inventory.Domain.Enums;

public enum CollectionStatus : byte
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
    Disabled = 4
}
