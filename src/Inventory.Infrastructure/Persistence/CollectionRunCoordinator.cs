using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class CollectionRunCoordinator(InventoryDbContext dbContext, IClock clock) : ICollectionRunCoordinator
{
    public async Task<CollectionRun?> TryLeaseSqlRunAsync(TimeSpan leaseTimeout, CancellationToken cancellationToken)
    {
        await dbContext.CollectionRuns
            .Where(x => x.Status == CollectionRunStatus.ProcessingSql && x.CompletedAtUtc == null && x.StartedAtUtc < clock.UtcNow - leaseTimeout)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, CollectionRunStatus.ReadyForSql), cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var run = await dbContext.CollectionRuns
            .Where(x => x.Status == CollectionRunStatus.ReadyForSql || x.Status == CollectionRunStatus.Partial)
            .OrderBy(x => x.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (run is not null)
        {
            run.Status = CollectionRunStatus.ProcessingSql;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return run;
    }
}
