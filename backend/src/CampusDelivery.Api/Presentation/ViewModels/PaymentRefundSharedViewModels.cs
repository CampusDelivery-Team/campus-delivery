namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class PaymentTaskStatusViewModel
{
    public int TaskId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public string TaskStatus { get; set; } = string.Empty;

    public string TaskStatusDisplayName { get; set; } = string.Empty;

    public bool CanPay { get; set; }

    public PaymentSummaryViewModel? Payment { get; set; }

    public RefundSummaryViewModel? Refund { get; set; }
}
