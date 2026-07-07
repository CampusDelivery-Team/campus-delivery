namespace CampusDelivery.Api.Models;

public sealed class Node
{
    public int NodeId { get; set; }

    public string NodeType { get; set; } = string.Empty;

    public string NodeName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string? OpenTime { get; set; }

    public string NodeStatus { get; set; } = "NORMAL";
}
