using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IServiceTypeRepository
{
    Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string serviceName, int? excludedServiceTypeId = null, CancellationToken cancellationToken = default);
    Task InsertAsync(ServiceType serviceType, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ServiceType serviceType, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int serviceTypeId, string typeStatus, CancellationToken cancellationToken = default);
    Task<ServiceTypeDeleteResult> DeleteAsync(int serviceTypeId, CancellationToken cancellationToken = default);
}
