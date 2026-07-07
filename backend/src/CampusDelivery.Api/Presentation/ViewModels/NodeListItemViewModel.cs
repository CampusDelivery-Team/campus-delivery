namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class NodeListItemViewModel
{
    public int NodeId { get; set; }

    public string NodeType { get; set; } = string.Empty;

    public string NodeTypeDisplayName { get; set; } = string.Empty;

    public string NodeName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string? OpenTime { get; set; }

    public string NodeStatus { get; set; } = "NORMAL";

    public string NodeStatusDisplayName { get; set; } = "正常";
}
