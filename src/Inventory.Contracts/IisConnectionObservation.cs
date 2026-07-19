namespace Inventory.Contracts;

public sealed record IisConnectionObservation(
    DateTime CollectedAtUtc,
    string IisServerName,
    string AppPoolName,
    int WorkerProcessId,
    string ClientIp,
    int ClientPort,
    string SqlServerIp,
    int SqlServerPort);
