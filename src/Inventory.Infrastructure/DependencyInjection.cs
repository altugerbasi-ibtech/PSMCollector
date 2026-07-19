using Inventory.Application.Abstractions;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICollectorCommandQueue, CollectorCommandStore>();
        services.AddScoped<ICollectionRunCoordinator, CollectionRunCoordinator>();
        services.AddScoped<IInventoryWriter, InventoryWriter>();
        return services;
    }
}
