using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Repositories;

public interface IServiceNodeRuleRepository
{
    List<ServiceNodeRuleViewModel> GetAllRules();
    bool Exists(int serviceTypeId, int nodeId);
    void AddRule(int serviceTypeId, int nodeId);
    void DeleteRule(int serviceTypeId, int nodeId);
}
