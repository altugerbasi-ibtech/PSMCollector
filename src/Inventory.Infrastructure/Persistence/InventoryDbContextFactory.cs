using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=IisSqlConnectionInventory;Integrated Security=True;Encrypt=False")
            .Options;

        return new InventoryDbContext(options);
    }
}
