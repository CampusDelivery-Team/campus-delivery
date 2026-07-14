namespace CampusDelivery.Api.Models;

public sealed class TaskStatusLog
{
    public int LogId { get; set; }
    public int RecordId { get; set; }
    public string? StatusBefore { get; set; }
    public string StatusAfter { get; set; } = string.Empty;
    public int OperatorUserId { get; set; }
    public DateTime OperatedAt { get; set; }
}
