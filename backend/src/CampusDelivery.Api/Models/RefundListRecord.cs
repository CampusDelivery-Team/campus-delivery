namespace CampusDelivery.Api.Models;

public sealed class RefundListRecord
{
    public int RefundId { get; set; }

    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal RefundAmount { get; set; }

    public string RefundStatus { get; set; } = "PENDING";

    public string? RefundReason { get; set; }

    public string? ReviewReason { get; set; }

    public string? ReviewedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
