namespace CampusRunnerSystem.ViewModels;

public class NodeViewModel
{
    public int NodeId { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string OpenTime { get; set; } = string.Empty;
    public string NodeStatus { get; set; } = string.Empty;
}
