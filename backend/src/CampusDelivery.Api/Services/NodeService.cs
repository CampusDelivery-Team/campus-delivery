using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;

namespace CampusDelivery.Api.Services;

public sealed class NodeService(NodeRepository nodeRepository)
{
    public async Task<NodeIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var nodes = await nodeRepository.GetAllAsync(cancellationToken);

        return new NodeIndexViewModel
        {
            Nodes = nodes.Select(ToListItem).ToList()
        };
    }

    public async Task<bool> CreateAsync(
        NodeCreateViewModel model,
        CancellationToken cancellationToken = default)
    {
        var nodeName = model.NodeName.Trim();
        if (await nodeRepository.ExistsByNameAsync(
                nodeName,
                cancellationToken: cancellationToken))
        {
            return false;
        }

        await nodeRepository.InsertAsync(ToNode(model), cancellationToken);
        return true;
    }

    public async Task<NodeUpdateResult> UpdateAsync(
        NodeEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var nodeName = model.NodeName.Trim();
        if (await nodeRepository.ExistsByNameAsync(
                nodeName,
                model.NodeId,
                cancellationToken))
        {
            return NodeUpdateResult.DuplicateName;
        }

        var node = ToNode(model);
        node.NodeId = model.NodeId;
        return await nodeRepository.UpdateAsync(node, cancellationToken)
            ? NodeUpdateResult.Success
            : NodeUpdateResult.NotFound;
    }

    public async Task<NodeStatusUpdateResult> UpdateStatusAsync(
        int nodeId,
        string nodeStatus,
        CancellationToken cancellationToken = default)
    {
        var node = await nodeRepository.GetByIdAsync(nodeId, cancellationToken);
        if (node is null)
        {
            return NodeStatusUpdateResult.NotFound;
        }

        if (node.NodeStatus == nodeStatus)
        {
            return NodeStatusUpdateResult.NoChange;
        }

        return await nodeRepository.UpdateStatusAsync(nodeId, nodeStatus, cancellationToken)
            ? NodeStatusUpdateResult.Success
            : NodeStatusUpdateResult.NoChange;
    }

    private static Node ToNode(NodeCreateViewModel model)
    {
        return new Node
        {
            NodeType = model.NodeType.Trim(),
            NodeName = model.NodeName.Trim(),
            Location = model.Location.Trim(),
            OpenTime = string.IsNullOrWhiteSpace(model.OpenTime) ? null : model.OpenTime.Trim(),
            NodeStatus = model.NodeStatus
        };
    }

    private static NodeListItemViewModel ToListItem(Node node)
    {
        return new NodeListItemViewModel
        {
            NodeId = node.NodeId,
            NodeType = node.NodeType,
            NodeTypeDisplayName = DisplayNameService.GetNodeTypeName(node.NodeType),
            NodeName = node.NodeName,
            Location = node.Location,
            OpenTime = node.OpenTime,
            NodeStatus = node.NodeStatus,
            NodeStatusDisplayName = DisplayNameService.GetNodeStatusName(node.NodeStatus)
        };
    }
}

public enum NodeUpdateResult
{
    Success,
    NotFound,
    DuplicateName
}

public enum NodeStatusUpdateResult
{
    Success,
    NotFound,
    NoChange
}
