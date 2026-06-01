using CampusRunnerSystem.Models;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public interface INodeService
{
    Result<List<NodeViewModel>> GetAllNodes();
    Result<NodeViewModel> GetNodeById(int nodeId);
    Result AddNode(NodeViewModel node);
    Result UpdateNode(NodeViewModel node);
    Result DeleteNode(int nodeId);
}
