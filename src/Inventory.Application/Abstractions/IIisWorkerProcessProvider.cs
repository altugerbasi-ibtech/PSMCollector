namespace Inventory.Application.Abstractions;

public interface IIisWorkerProcessProvider
{
    Task<IReadOnlyDictionary<int, string>> GetAppPoolsByProcessIdAsync(string hostName, CancellationToken cancellationToken);
}
