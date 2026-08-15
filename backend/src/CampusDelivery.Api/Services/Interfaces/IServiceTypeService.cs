using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IServiceTypeService
{
    Task<ServiceTypeIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(ServiceTypeCreateViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceTypeUpdateResult> UpdateAsync(ServiceTypeEditViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int serviceTypeId, string typeStatus, CancellationToken cancellationToken = default);
    Task<ServiceTypeDeleteOperationResult> DeleteAsync(int serviceTypeId, CancellationToken cancellationToken = default);
}
