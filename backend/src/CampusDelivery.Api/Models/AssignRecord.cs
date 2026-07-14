namespace CampusDelivery.Api.Models;

public sealed class AssignRecord
{
    public int RecordId { get; set; }
    public int TaskId { get; set; }
    public int RunnerId { get; set; }
    public string OperationType { get; set; } = "SELF";
    public DateTime AssignedAt { get; set; }
    public string? ReassignReason { get; set; }
}
