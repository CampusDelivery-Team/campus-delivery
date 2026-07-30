namespace CampusDelivery.Api.Models;

public sealed class PaymentRecord
{
    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public int RecordId { get; set; }

    public int PublisherUserId { get; set; }

    public decimal OrderAmount { get; set; }

    public decimal PayAmount { get; set; }

    public string PayMethod { get; set; } = "CASH";

    public string PayStatus { get; set; } = "UNPAID";

    public string? ThirdTradeNo { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
