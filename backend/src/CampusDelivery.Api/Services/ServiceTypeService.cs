using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class ServiceTypeService(IServiceTypeRepository serviceTypeRepository) : IServiceTypeService
{
    public async Task<ServiceTypeIndexViewModel> GetIndexAsync(
        CancellationToken cancellationToken = default)
    {
        var serviceTypes = await serviceTypeRepository.GetAllAsync(cancellationToken);

        return new ServiceTypeIndexViewModel
        {
            ServiceTypes = serviceTypes.Select(ToListItem).ToList()
        };
    }

    public async Task<bool> CreateAsync(
        ServiceTypeCreateViewModel model,
        CancellationToken cancellationToken = default)
    {
        var serviceName = model.ServiceName.Trim();
        if (await serviceTypeRepository.ExistsByNameAsync(
                serviceName,
                cancellationToken: cancellationToken))
        {
            return false;
        }

        var serviceType = new ServiceType
        {
            ServiceName = serviceName,
            BasePrice = model.BasePrice!.Value,
            DistanceRule = NormalizeOptionalText(model.DistanceRule),
            UrgentRule = NormalizeOptionalText(model.UrgentRule),
            TypeStatus = model.TypeStatus
        };

        ServiceTypeRepositoryWriteResult result = await serviceTypeRepository.InsertAsync(
            serviceType,
            cancellationToken);
        return result == ServiceTypeRepositoryWriteResult.Success;
    }

    public async Task<ServiceTypeUpdateResult> UpdateAsync(
        ServiceTypeEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var serviceName = model.ServiceName.Trim();
        if (await serviceTypeRepository.ExistsByNameAsync(
                serviceName,
                model.ServiceTypeId,
                cancellationToken))
        {
            return ServiceTypeUpdateResult.DuplicateName;
        }

        var serviceType = new ServiceType
        {
            ServiceTypeId = model.ServiceTypeId,
            ServiceName = serviceName,
            BasePrice = model.BasePrice!.Value,
            DistanceRule = NormalizeOptionalText(model.DistanceRule),
            UrgentRule = NormalizeOptionalText(model.UrgentRule),
            TypeStatus = model.TypeStatus
        };

        ServiceTypeRepositoryWriteResult result = await serviceTypeRepository.UpdateAsync(
            serviceType,
            cancellationToken);
        return result switch
        {
            ServiceTypeRepositoryWriteResult.Success => ServiceTypeUpdateResult.Success,
            ServiceTypeRepositoryWriteResult.DuplicateName => ServiceTypeUpdateResult.DuplicateName,
            _ => ServiceTypeUpdateResult.NotFound
        };
    }

    public Task<bool> UpdateStatusAsync(
        int serviceTypeId,
        string typeStatus,
        CancellationToken cancellationToken = default)
    {
        return serviceTypeRepository.UpdateStatusAsync(
            serviceTypeId,
            typeStatus,
            cancellationToken);
    }

    public async Task<ServiceTypeDeleteOperationResult> DeleteAsync(
        int serviceTypeId,
        CancellationToken cancellationToken = default)
    {
        ServiceTypeDeleteResult result = await serviceTypeRepository.DeleteAsync(serviceTypeId, cancellationToken);
        return result switch
        {
            ServiceTypeDeleteResult.Success => ServiceTypeDeleteOperationResult.Success,
            ServiceTypeDeleteResult.Referenced => ServiceTypeDeleteOperationResult.Referenced,
            _ => ServiceTypeDeleteOperationResult.NotFound
        };
    }

    private static ServiceTypeListItemViewModel ToListItem(ServiceType serviceType)
    {
        return new ServiceTypeListItemViewModel
        {
            ServiceTypeId = serviceType.ServiceTypeId,
            ServiceName = serviceType.ServiceName,
            BasePrice = serviceType.BasePrice,
            DistanceRule = serviceType.DistanceRule,
            UrgentRule = serviceType.UrgentRule,
            TypeStatus = serviceType.TypeStatus,
            TypeStatusDisplayName = DisplayNameService.GetServiceTypeStatusName(serviceType.TypeStatus)
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
