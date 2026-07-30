namespace CampusDelivery.Api.Models;

public sealed class RefundRecord
{
    public int RefundId { get; set; }

    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public int RecordId { get; set; }

    public int RequestUserId { get; set; }

    public decimal RefundAmount { get; set; }

    public string RefundStatus { get; set; } = "PENDING";

    public string? RefundReason { get; set; }

    public string? ReviewReason { get; set; }

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
