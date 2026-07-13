namespace CampusDelivery.Api.Models;

public sealed class ServiceNodeRule
{
    public int ServiceTypeId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceTypeStatus { get; set; } = "ENABLED";

    public int NodeId { get; set; }

    public string NodeName { get; set; } = string.Empty;

    public string NodeType { get; set; } = string.Empty;

    public string NodeStatus { get; set; } = "NORMAL";

    public bool IsReferenced { get; set; }
}
