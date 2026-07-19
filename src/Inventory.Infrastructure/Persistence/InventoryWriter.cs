using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryWriter(InventoryDbContext dbContext) : IInventoryWriter
{
    public async Task WriteAsync(Guid collectionId, IReadOnlyCollection<ConnectionInventory> rows, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.ConnectionInventory.Where(x => x.CollectionId == collectionId).ExecuteDeleteAsync(cancellationToken);
        dbContext.ConnectionInventory.AddRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.IisConnectionStaging.Where(x => x.CollectionId == collectionId).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
