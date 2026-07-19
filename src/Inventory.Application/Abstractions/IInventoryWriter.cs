using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface IInventoryWriter
{
    Task WriteAsync(Guid collectionId, IReadOnlyCollection<ConnectionInventory> rows, CancellationToken cancellationToken);
}
