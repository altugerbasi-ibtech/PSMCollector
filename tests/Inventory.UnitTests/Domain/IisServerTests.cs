using Inventory.Domain.Entities;

namespace Inventory.UnitTests.Domain;

public sealed class IisServerTests
{
    [Fact]
    public void IsDueReturnsTrueWhenNeverAttemptedAndEnabled()
    {
        var server = CreateServer();

        Assert.True(server.IsDue(DateTime.UtcNow));
    }

    [Fact]
    public void IsDueReturnsFalseUntilCollectionIntervalElapses()
    {
        var now = new DateTime(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);
        var server = CreateServer();
        server.CollectionIntervalSeconds = 60;
        server.LastCollectionAttemptUtc = now.AddSeconds(-59);

        Assert.False(server.IsDue(now));
        Assert.True(server.IsDue(now.AddSeconds(1)));
    }

    [Fact]
    public void IsDueReturnsFalseWhenDisabled()
    {
        var server = CreateServer();
        server.IsEnabled = false;

        Assert.False(server.IsDue(DateTime.UtcNow));
    }

    private static IisServer CreateServer() => new()
    {
        ServerName = "IIS01",
        CreatedBy = "test",
        UpdatedBy = "test"
    };
}
