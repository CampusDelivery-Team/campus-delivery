using CampusRunnerSystem.Models;
using CampusRunnerSystem.Repositories;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public class ServiceNodeRuleService : IServiceNodeRuleService
{
    private readonly IServiceNodeRuleRepository _ruleRepository;
    private readonly IServiceTypeRepository _serviceTypeRepository;
    private readonly INodeRepository _nodeRepository;

    public ServiceNodeRuleService(
        IServiceNodeRuleRepository ruleRepository,
        IServiceTypeRepository serviceTypeRepository,
        INodeRepository nodeRepository)
    {
        _ruleRepository = ruleRepository;
        _serviceTypeRepository = serviceTypeRepository;
        _nodeRepository = nodeRepository;
    }

    public Result<List<ServiceNodeRuleViewModel>> GetAllRules()
    {
        try
        {
            return Result<List<ServiceNodeRuleViewModel>>.Ok(_ruleRepository.GetAllRules());
        }
        catch (Exception ex)
        {
            return Result<List<ServiceNodeRuleViewModel>>.Fail($"服务节点规则读取失败：{ex.Message}");
        }
    }

    public Result AddRule(int serviceTypeId, int nodeId)
    {
        try
        {
            if (_serviceTypeRepository.GetServiceTypeById(serviceTypeId) == null)
            {
                return Result.Fail("请选择有效的服务类型。");
            }

            if (_nodeRepository.GetNodeById(nodeId) == null)
            {
                return Result.Fail("请选择有效的节点。");
            }

            if (_ruleRepository.Exists(serviceTypeId, nodeId))
            {
                return Result.Fail("该服务类型与节点已存在绑定关系。");
            }

            _ruleRepository.AddRule(serviceTypeId, nodeId);
            return Result.Ok("服务节点规则绑定成功。");
        }
        catch (Exception ex)
        {
            return Result.Fail($"服务节点规则绑定失败：{ex.Message}");
        }
    }

    public Result DeleteRule(int serviceTypeId, int nodeId)
    {
        try
        {
            _ruleRepository.DeleteRule(serviceTypeId, nodeId);
            return Result.Ok("服务节点规则删除成功。");
        }
        catch (Exception)
        {
            return Result.Fail("服务节点规则删除失败，该规则可能已被任务引用。");
        }
    }
}
