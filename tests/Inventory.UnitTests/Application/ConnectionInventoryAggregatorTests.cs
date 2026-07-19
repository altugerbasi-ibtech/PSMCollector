using Inventory.Application.Services;
using Inventory.Contracts;
using Inventory.Domain.Entities;

namespace Inventory.UnitTests.Application;

public sealed class ConnectionInventoryAggregatorTests
{
    [Fact]
    public void AggregateMatchesSocketAndCountsDistinctSessions()
    {
        var iis = new IisServer { Id = 1, ServerName = "IIS01", CreatedBy = "test", UpdatedBy = "test" };
        var sql = new SqlServer { Id = 2, ServerName = "SQL01", Fqdn = "sql01.ae.local", IpAddress = "10.0.0.2", CreatedBy = "test", UpdatedBy = "test" };
        var observed = new[] { new IisConnectionObservation(DateTime.UtcNow, "IIS01", "PoolA", 42, "10.0.0.1", 51000, "10.0.0.2", 1433) };
        var sessions = new[]
        {
            new SqlSessionSnapshot("10.0.0.1", 51000, 7, "AppDb", true),
            new SqlSessionSnapshot("10.0.0.1", 51000, 7, "AppDb", true)
        };

        var result = Assert.Single(ConnectionInventoryAggregator.Aggregate(Guid.NewGuid(), DateTime.UtcNow, iis, sql, observed, sessions));

        Assert.Equal(1, result.TotalConnections);
        Assert.Equal(1, result.ActiveConnections);
        Assert.Equal(0, result.IdlePooledConnections);
    }

    [Fact]
    public void AggregateIgnoresNonStandardSqlPortsAndUnmatchedSockets()
    {
        var iis = new IisServer { Id = 1, ServerName = "IIS01", CreatedBy = "test", UpdatedBy = "test" };
        var sql = new SqlServer { Id = 2, ServerName = "SQL01", Fqdn = "sql01.ae.local", IpAddress = "10.0.0.2", CreatedBy = "test", UpdatedBy = "test" };
        var observed = new[] { new IisConnectionObservation(DateTime.UtcNow, "IIS01", "PoolA", 42, "10.0.0.1", 51000, "10.0.0.2", 1444) };

        Assert.Empty(ConnectionInventoryAggregator.Aggregate(Guid.NewGuid(), DateTime.UtcNow, iis, sql, observed, []));
    }
}
