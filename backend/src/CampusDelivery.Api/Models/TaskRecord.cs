namespace CampusDelivery.Api.Models;

public sealed class TaskRecord
{
    public int TaskId { get; set; }

    public string PublisherUsername { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string Campus { get; set; } = string.Empty;

    public string BuildingRoom { get; set; } = string.Empty;

    public string NodeName { get; set; } = string.Empty;

    public string TaskTitle { get; set; } = string.Empty;

    public decimal TaskPrice { get; set; }

    public string UrgentFlag { get; set; } = "N";

    public string TaskStatus { get; set; } = "WAITING";

    public DateTime CreatedAt { get; set; }

    public string TaskKind { get; set; } = string.Empty;
}
