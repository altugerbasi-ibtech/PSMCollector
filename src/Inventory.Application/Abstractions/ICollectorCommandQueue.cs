using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

#pragma warning disable CA1711 // Required domain abstraction name from the binding specification.

namespace Inventory.Application.Abstractions;

public interface ICollectorCommandQueue
{
    Task<Guid> EnqueueAsync(CollectorType collector, CollectorCommandType command, int targetId, string requestedBy, CancellationToken cancellationToken);
    Task<CollectorCommand?> TryLeaseAsync(CollectorType collector, CancellationToken cancellationToken);
    Task CompleteAsync(Guid commandId, bool succeeded, string result, CancellationToken cancellationToken);
}
#pragma warning restore CA1711
