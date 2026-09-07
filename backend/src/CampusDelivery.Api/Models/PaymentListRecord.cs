namespace CampusDelivery.Api.Models;

public sealed class PaymentListRecord
{
    public int PaymentId { get; set; }
    public int TaskId { get; set; }
    public int RecordId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public decimal PayAmount { get; set; }
    public string PayMethod { get; set; } = "CASH";
    public string? ThirdTradeNo { get; set; }
    public string PayStatus { get; set; } = "UNPAID";
    public string? RefundProcessStatus { get; set; }
}
