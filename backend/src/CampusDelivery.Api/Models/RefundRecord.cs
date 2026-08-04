namespace CampusDelivery.Api.Models;

public sealed class RefundRecord
{
    public int RefundId { get; set; }
    public int PaymentId { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundReason { get; set; } = string.Empty;
    public decimal? ApprovedAmount { get; set; }
    public string ProcessStatus { get; set; } = "APPLY";
}
