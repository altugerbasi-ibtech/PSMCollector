namespace Inventory.Domain.Enums;

public enum CollectionRunStatus : byte
{
    PendingIis = 0,
    ReadyForSql = 1,
    ProcessingSql = 2,
    Completed = 3,
    Partial = 4,
    Failed = 5,
    Expired = 6
}
