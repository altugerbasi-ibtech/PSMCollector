using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class CollectorCommandStore(InventoryDbContext dbContext, IClock clock) : ICollectorCommandQueue
{
    public async Task<Guid> EnqueueAsync(CollectorType collector, CollectorCommandType command, int targetId, string requestedBy, CancellationToken cancellationToken)
    {
        var item = new CollectorCommand { Id = Guid.NewGuid(), CollectorType = collector, CommandType = command, TargetId = targetId, RequestedAtUtc = clock.UtcNow, RequestedBy = requestedBy };
        dbContext.CollectorCommands.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<CollectorCommand?> TryLeaseAsync(CollectorType collector, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var item = await dbContext.CollectorCommands
            .Where(x => x.CollectorType == collector && x.Status == CollectorCommandStatus.Pending)
            .OrderBy(x => x.RequestedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (item is not null)
        {
            item.Status = CollectorCommandStatus.Processing;
            item.ProcessingStartedAtUtc = clock.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return item;
    }

    public async Task CompleteAsync(Guid commandId, bool succeeded, string result, CancellationToken cancellationToken)
    {
        var item = await dbContext.CollectorCommands.SingleAsync(x => x.Id == commandId, cancellationToken);
        item.Status = succeeded ? CollectorCommandStatus.Succeeded : CollectorCommandStatus.Failed;
        item.CompletedAtUtc = clock.UtcNow;
        item.ResultMessage = result.Length <= 2000 ? result : result[..2000];
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
