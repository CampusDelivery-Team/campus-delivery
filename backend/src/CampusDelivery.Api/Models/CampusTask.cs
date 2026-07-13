namespace CampusDelivery.Api.Models;

public sealed class CampusTask
{
    public int TaskId { get; set; }
    public int PublisherUserId { get; set; }
    public int ServiceTypeId { get; set; }
    public int AddressNo { get; set; }
    public int NodeId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal TaskPrice { get; set; }
    public string UrgentFlag { get; set; } = "N";
    public string TaskStatus { get; set; } = "CREATED";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
