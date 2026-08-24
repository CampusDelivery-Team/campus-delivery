using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IServiceNodeRuleRepository
{
    Task<IReadOnlyList<ServiceNodeRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default);
    Task<bool> InsertAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default);
    Task<ServiceNodeRuleRemoveResult> RemoveAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default);
}
