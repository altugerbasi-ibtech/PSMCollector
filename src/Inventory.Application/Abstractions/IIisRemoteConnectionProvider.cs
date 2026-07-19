using Inventory.Contracts;
using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface IIisRemoteConnectionProvider
{
    Task<IReadOnlyList<IisConnectionObservation>> CollectAsync(IisServer server, CancellationToken cancellationToken);
    Task TestAsync(IisServer server, CancellationToken cancellationToken);
}
