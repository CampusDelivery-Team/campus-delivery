using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IServiceNodeRuleService
{
    Task<ServiceNodeRuleIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<ServiceNodeRuleCreateResult> CreateAsync(ServiceNodeRuleCreateViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceNodeRuleRemoveResult> RemoveAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default);
}
