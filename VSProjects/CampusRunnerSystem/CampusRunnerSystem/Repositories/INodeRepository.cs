using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Repositories;

public interface INodeRepository
{
    List<NodeViewModel> GetAllNodes();
    NodeViewModel? GetNodeById(int nodeId);
    void AddNode(NodeViewModel node);
    void UpdateNode(NodeViewModel node);
    void DeleteNode(int nodeId);
    int CountTaskReferences(int nodeId);
    int CountRuleReferences(int nodeId);
}
