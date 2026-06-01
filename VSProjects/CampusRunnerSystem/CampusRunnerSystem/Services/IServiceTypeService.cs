using CampusRunnerSystem.Models;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public interface IServiceTypeService
{
    Result<List<ServiceTypeViewModel>> GetAllServiceTypes();
    Result<ServiceTypeViewModel> GetServiceTypeById(int serviceTypeId);
    Result AddServiceType(ServiceTypeViewModel serviceType);
    Result UpdateServiceType(ServiceTypeViewModel serviceType);
    Result DeleteServiceType(int serviceTypeId);
}
