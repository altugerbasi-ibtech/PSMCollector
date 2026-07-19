namespace Inventory.Contracts;

public sealed record SqlSessionSnapshot(
    string ClientIp,
    int ClientPort,
    int SessionId,
    string DatabaseName,
    bool HasActiveRequest);
