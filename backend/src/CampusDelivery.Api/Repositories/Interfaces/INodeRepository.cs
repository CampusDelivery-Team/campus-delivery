using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface INodeRepository
{
    Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Node?> GetByIdAsync(int nodeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string nodeName, int? excludedNodeId = null, CancellationToken cancellationToken = default);
    Task InsertAsync(Node node, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Node node, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int nodeId, string nodeStatus, CancellationToken cancellationToken = default);
    Task<NodeDeleteResult> DeleteAsync(int nodeId, CancellationToken cancellationToken = default);
}
