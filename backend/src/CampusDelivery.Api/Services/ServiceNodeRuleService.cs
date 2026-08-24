using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;
using RepositoryRemoveResult = CampusDelivery.Api.Repositories.Interfaces.ServiceNodeRuleRemoveResult;
using ServiceRemoveResult = CampusDelivery.Api.Services.Interfaces.ServiceNodeRuleRemoveResult;

namespace CampusDelivery.Api.Services;

public sealed class ServiceNodeRuleService(
    IServiceNodeRuleRepository ruleRepository,
    IServiceTypeRepository serviceTypeRepository,
    INodeRepository nodeRepository) : IServiceNodeRuleService
{
    public async Task<ServiceNodeRuleIndexViewModel> GetIndexAsync(
        CancellationToken cancellationToken = default)
    {
        var rulesTask = ruleRepository.GetAllAsync(cancellationToken);
        var serviceTypesTask = serviceTypeRepository.GetAllAsync(cancellationToken);
        var nodesTask = nodeRepository.GetAllAsync(cancellationToken);

        await Task.WhenAll(rulesTask, serviceTypesTask, nodesTask);

        return new ServiceNodeRuleIndexViewModel
        {
            Rules = rulesTask.Result.Select(rule => new ServiceNodeRuleListItemViewModel
            {
                ServiceTypeId = rule.ServiceTypeId,
                ServiceName = rule.ServiceName,
                ServiceTypeStatus = rule.ServiceTypeStatus,
                ServiceTypeStatusDisplayName = DisplayNameService.GetServiceTypeStatusName(rule.ServiceTypeStatus),
                NodeId = rule.NodeId,
                NodeName = rule.NodeName,
                NodeType = rule.NodeType,
                NodeTypeDisplayName = DisplayNameService.GetNodeTypeName(rule.NodeType),
                NodeStatus = rule.NodeStatus,
                NodeStatusDisplayName = DisplayNameService.GetNodeStatusName(rule.NodeStatus),
                CanRemove = !rule.IsReferenced
            }).ToList(),
            ServiceTypes = serviceTypesTask.Result
                .Where(serviceType => serviceType.TypeStatus == "ENABLED")
                .Select(serviceType => new ServiceNodeRuleOptionViewModel
                {
                    Id = serviceType.ServiceTypeId,
                    DisplayName = $"{serviceType.ServiceName}（#{serviceType.ServiceTypeId}）"
                }).ToList(),
            Nodes = nodesTask.Result
                .Where(node => node.NodeStatus == "NORMAL")
                .Select(node => new ServiceNodeRuleOptionViewModel
                {
                    Id = node.NodeId,
                    DisplayName = $"{node.NodeName} · {DisplayNameService.GetNodeTypeName(node.NodeType)}（#{node.NodeId}）"
                }).ToList()
        };
    }

    public async Task<ServiceNodeRuleCreateResult> CreateAsync(
        ServiceNodeRuleCreateViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (await ruleRepository.ExistsAsync(
                model.ServiceTypeId,
                model.NodeId,
                cancellationToken))
        {
            return ServiceNodeRuleCreateResult.Duplicate;
        }

        if (await ruleRepository.InsertAsync(
                model.ServiceTypeId,
                model.NodeId,
                cancellationToken))
        {
            return ServiceNodeRuleCreateResult.Success;
        }

        return await ruleRepository.ExistsAsync(
            model.ServiceTypeId,
            model.NodeId,
            cancellationToken)
            ? ServiceNodeRuleCreateResult.Duplicate
            : ServiceNodeRuleCreateResult.Unavailable;
    }

    public async Task<ServiceRemoveResult> RemoveAsync(
        int serviceTypeId,
        int nodeId,
        CancellationToken cancellationToken = default)
    {
        var writeResult = await ruleRepository.RemoveAsync(
            serviceTypeId,
            nodeId,
            cancellationToken);

        return writeResult switch
        {
            RepositoryRemoveResult.Success => ServiceRemoveResult.Success,
            RepositoryRemoveResult.Referenced => ServiceRemoveResult.Referenced,
            _ => ServiceRemoveResult.NotFound
        };
    }
}
