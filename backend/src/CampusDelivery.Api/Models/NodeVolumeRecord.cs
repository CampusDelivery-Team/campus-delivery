namespace CampusDelivery.Api.Models;

public sealed class NodeVolumeRecord
{
    public int NodeId { get; set; }

    public string NodeName { get; set; } = string.Empty;

    public int TaskCount { get; set; }

    public int FinishedTaskCount { get; set; }
}

