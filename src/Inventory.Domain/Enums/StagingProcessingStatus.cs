namespace Inventory.Domain.Enums;

public enum StagingProcessingStatus : byte
{
    Pending = 0,
    Processing = 1,
    Failed = 2,
    Expired = 3
}
