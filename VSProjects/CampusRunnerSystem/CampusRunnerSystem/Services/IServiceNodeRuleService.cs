using CampusRunnerSystem.Models;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public interface IServiceNodeRuleService
{
    Result<List<ServiceNodeRuleViewModel>> GetAllRules();
    Result AddRule(int serviceTypeId, int nodeId);
    Result DeleteRule(int serviceTypeId, int nodeId);
}
