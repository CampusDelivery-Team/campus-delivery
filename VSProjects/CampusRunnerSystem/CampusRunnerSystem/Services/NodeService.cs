using CampusRunnerSystem.Models;
using CampusRunnerSystem.Repositories;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public class NodeService : INodeService
{
    private readonly INodeRepository _nodeRepository;

    public NodeService(INodeRepository nodeRepository)
    {
        _nodeRepository = nodeRepository;
    }

    public Result<List<NodeViewModel>> GetAllNodes()
    {
        try
        {
            return Result<List<NodeViewModel>>.Ok(_nodeRepository.GetAllNodes());
        }
        catch (Exception ex)
        {
            return Result<List<NodeViewModel>>.Fail($"节点数据读取失败：{ex.Message}");
        }
    }

    public Result<NodeViewModel> GetNodeById(int nodeId)
    {
        try
        {
            var node = _nodeRepository.GetNodeById(nodeId);
            return node == null
                ? Result<NodeViewModel>.Fail("未找到该节点。")
                : Result<NodeViewModel>.Ok(node);
        }
        catch (Exception ex)
        {
            return Result<NodeViewModel>.Fail($"节点详情读取失败：{ex.Message}");
        }
    }

    public Result AddNode(NodeViewModel node)
    {
        var check = ValidateNode(node);
        if (!check.Success)
        {
            return check;
        }

        try
        {
            _nodeRepository.AddNode(node);
            return Result.Ok("节点新增成功。");
        }
        catch (Exception ex)
        {
            return Result.Fail($"节点新增失败：{ex.Message}");
        }
    }

    public Result UpdateNode(NodeViewModel node)
    {
        var check = ValidateNode(node);
        if (!check.Success)
        {
            return check;
        }

        try
        {
            _nodeRepository.UpdateNode(node);
            return Result.Ok("节点修改成功。");
        }
        catch (Exception ex)
        {
            return Result.Fail($"节点修改失败：{ex.Message}");
        }
    }

    public Result DeleteNode(int nodeId)
    {
        try
        {
            if (_nodeRepository.CountTaskReferences(nodeId) > 0)
            {
                return Result.Fail("该节点已被任务引用，不能删除。");
            }

            if (_nodeRepository.CountRuleReferences(nodeId) > 0)
            {
                return Result.Fail("该节点已被服务节点规则引用，请先删除绑定关系。");
            }

            _nodeRepository.DeleteNode(nodeId);
            return Result.Ok("节点删除成功。");
        }
        catch (Exception)
        {
            return Result.Fail("节点删除失败，该节点可能已被其他业务数据引用。");
        }
    }

    private static Result ValidateNode(NodeViewModel node)
    {
        if (string.IsNullOrWhiteSpace(node.NodeType)
            || string.IsNullOrWhiteSpace(node.NodeName)
            || string.IsNullOrWhiteSpace(node.Location))
        {
            return Result.Fail("请填写节点类型、节点名称和具体位置。");
        }

        if (node.NodeStatus != SystemConstants.NodeStatus.Normal
            && node.NodeStatus != SystemConstants.NodeStatus.Closed)
        {
            return Result.Fail("节点状态只能是“正常”或“关闭”。");
        }

        return Result.Ok();
    }
}
