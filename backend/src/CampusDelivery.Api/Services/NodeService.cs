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
            Nodes = nodes.Select(ToListItem).ToList(),
            IsReadOnly = true
        };
    }

    public async Task<NodeEditViewModel?> GetEditModelAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        var node = await nodeRepository.GetByIdAsync(nodeId, cancellationToken);
        if (node is null)
        {
            return null;
        }

        return new NodeEditViewModel
        {
            NodeId = node.NodeId,
            NodeType = node.NodeType,
            NodeName = node.NodeName,
            Location = node.Location,
            OpenTime = node.OpenTime,
            NodeStatus = node.NodeStatus
        };
    }

    public async Task CreateAsync(NodeCreateViewModel model, CancellationToken cancellationToken = default)
    {
        await nodeRepository.InsertAsync(ToNode(model), cancellationToken);
    }

    public async Task<bool> UpdateAsync(NodeEditViewModel model, CancellationToken cancellationToken = default)
    {
        var existing = await nodeRepository.GetByIdAsync(model.NodeId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var node = ToNode(model);
        node.NodeId = model.NodeId;
        await nodeRepository.UpdateAsync(node, cancellationToken);
        return true;
    }

    public Task DeleteAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        return nodeRepository.DeleteAsync(nodeId, cancellationToken);
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
