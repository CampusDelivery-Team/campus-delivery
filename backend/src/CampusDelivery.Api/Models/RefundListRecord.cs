namespace CampusDelivery.Api.Models;

public sealed class RefundListRecord
{
    public int RefundId { get; set; }
    public int PaymentId { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string? RefundReason { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string ProcessStatus { get; set; } = "APPLY";
}
