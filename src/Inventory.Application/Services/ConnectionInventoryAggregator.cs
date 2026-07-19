using Inventory.Contracts;
using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public static class ConnectionInventoryAggregator
{
    public static IReadOnlyList<ConnectionInventory> Aggregate(
        Guid collectionId,
        DateTime inventoryDateUtc,
        IisServer iisServer,
        SqlServer sqlServer,
        IEnumerable<IisConnectionObservation> observations,
        IEnumerable<SqlSessionSnapshot> sessions)
    {
        var sessionLookup = sessions
            .GroupBy(x => (x.ClientIp, x.ClientPort))
            .ToDictionary(x => x.Key, x => x.GroupBy(s => s.SessionId).Select(g => g.First()).ToArray());

        return observations
            .Where(x => x.SqlServerPort == 1433)
            .SelectMany(observation => sessionLookup.TryGetValue((observation.ClientIp, observation.ClientPort), out var matches)
                ? matches.Select(session => (observation, session))
                : [])
            .GroupBy(x => new { x.observation.AppPoolName, x.session.DatabaseName })
            .Select(group =>
            {
                var distinctSessions = group.GroupBy(x => x.session.SessionId).Select(x => x.First()).ToArray();
                var active = distinctSessions.Count(x => x.session.HasActiveRequest);
                return new ConnectionInventory
                {
                    CollectionId = collectionId,
                    InventoryDateUtc = inventoryDateUtc,
                    IisServerId = iisServer.Id,
                    IisServerName = iisServer.ServerName,
                    AppPoolName = group.Key.AppPoolName,
                    SqlServerId = sqlServer.Id,
                    SqlServerName = sqlServer.ServerName,
                    DatabaseName = group.Key.DatabaseName,
                    SqlEndpoint = $"{sqlServer.Fqdn}:1433",
                    TotalConnections = distinctSessions.Length,
                    ActiveConnections = active,
                    IdlePooledConnections = distinctSessions.Length - active,
                    WorkerProcessIds = string.Join(',', group.Select(x => x.observation.WorkerProcessId).Distinct().Order())
                };
            })
            .ToArray();
    }
}
