using Inventory.Domain.Entities;

namespace Inventory.UnitTests.Domain;

public sealed class ConnectionInventoryTests
{
    [Theory]
    [InlineData(5, 2, 3, true)]
    [InlineData(5, 2, 2, false)]
    public void HasValidTotalsEnforcesActiveAndIdleSum(int total, int active, int idle, bool expected)
    {
        var inventory = new ConnectionInventory
        {
            IisServerName = "IIS01",
            AppPoolName = "DefaultAppPool",
            SqlServerName = "SQL01",
            DatabaseName = "AppDb",
            SqlEndpoint = "sql01.example:1433",
            TotalConnections = total,
            ActiveConnections = active,
            IdlePooledConnections = idle
        };

        Assert.Equal(expected, inventory.HasValidTotals);
    }
}
