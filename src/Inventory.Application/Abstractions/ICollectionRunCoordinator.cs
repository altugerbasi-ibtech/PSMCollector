using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface ICollectionRunCoordinator
{
    Task<CollectionRun?> TryLeaseSqlRunAsync(TimeSpan leaseTimeout, CancellationToken cancellationToken);
}
