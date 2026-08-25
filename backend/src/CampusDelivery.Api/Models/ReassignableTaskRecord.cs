namespace CampusDelivery.Api.Models;

public sealed class ReassignableTaskRecord
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskStatus { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CurrentRunnerId { get; set; }
    public string CurrentRunnerName { get; set; } = string.Empty;
}
