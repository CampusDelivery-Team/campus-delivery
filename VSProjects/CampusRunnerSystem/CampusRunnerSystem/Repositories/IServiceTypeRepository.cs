using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Repositories;

public interface IServiceTypeRepository
{
    List<ServiceTypeViewModel> GetAllServiceTypes();
    ServiceTypeViewModel? GetServiceTypeById(int serviceTypeId);
    void AddServiceType(ServiceTypeViewModel serviceType);
    void UpdateServiceType(ServiceTypeViewModel serviceType);
    void DeleteServiceType(int serviceTypeId);
    int CountTaskReferences(int serviceTypeId);
    int CountRuleReferences(int serviceTypeId);
}
