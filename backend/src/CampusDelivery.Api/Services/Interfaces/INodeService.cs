using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface INodeService
{
    Task<NodeIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(NodeCreateViewModel model, CancellationToken cancellationToken = default);
    Task<NodeUpdateResult> UpdateAsync(NodeEditViewModel model, CancellationToken cancellationToken = default);
    Task<NodeStatusUpdateResult> UpdateStatusAsync(int nodeId, string nodeStatus, CancellationToken cancellationToken = default);
    Task<NodeDeleteOperationResult> DeleteAsync(int nodeId, CancellationToken cancellationToken = default);
}
