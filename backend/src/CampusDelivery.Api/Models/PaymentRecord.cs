namespace CampusDelivery.Api.Models;

public sealed class PaymentRecord
{
    public int PaymentId { get; set; }

    // 以下三个字段由 payments.record_id 关联 assign_records、tasks 后得到，不是 payments 的列。
    public int TaskId { get; set; }
    public int RecordId { get; set; }
    public int PublisherUserId { get; set; }

    public decimal OrderAmount { get; set; }
    public decimal PayAmount { get; set; }
    public string PayMethod { get; set; } = "CASH";
    public string? ThirdTradeNo { get; set; }
    public string PayStatus { get; set; } = "UNPAID";
}
