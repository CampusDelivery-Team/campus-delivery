namespace CampusDelivery.Api.Models;

public sealed class Complaint
{
    public int ComplaintId { get; set; }
    public int RecordId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ProcessStatus { get; set; } = "SUBMITTED";
    public string? ProcessResult { get; set; }
}
