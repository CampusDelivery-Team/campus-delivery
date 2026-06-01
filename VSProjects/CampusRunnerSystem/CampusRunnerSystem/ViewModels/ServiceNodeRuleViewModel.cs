namespace CampusRunnerSystem.ViewModels;

public class ServiceNodeRuleViewModel
{
    public int ServiceTypeId { get; set; }
    public int NodeId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
}
