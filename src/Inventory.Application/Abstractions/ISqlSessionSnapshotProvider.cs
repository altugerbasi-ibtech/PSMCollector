using Inventory.Contracts;
using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface ISqlSessionSnapshotProvider
{
    Task<IReadOnlyList<SqlSessionSnapshot>> GetSnapshotAsync(SqlServer server, IReadOnlyCollection<IisConnectionObservation> connections, CancellationToken cancellationToken);
    Task TestAsync(SqlServer server, CancellationToken cancellationToken);
}
