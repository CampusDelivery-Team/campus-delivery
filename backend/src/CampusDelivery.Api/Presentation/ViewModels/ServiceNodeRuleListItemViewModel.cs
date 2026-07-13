namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ServiceNodeRuleListItemViewModel
{
    public int ServiceTypeId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceTypeStatus { get; set; } = "ENABLED";

    public string ServiceTypeStatusDisplayName { get; set; } = "启用";

    public int NodeId { get; set; }

    public string NodeName { get; set; } = string.Empty;

    public string NodeType { get; set; } = string.Empty;

    public string NodeTypeDisplayName { get; set; } = string.Empty;

    public string NodeStatus { get; set; } = "NORMAL";

    public string NodeStatusDisplayName { get; set; } = "正常";

    public bool CanRemove { get; set; }
}
