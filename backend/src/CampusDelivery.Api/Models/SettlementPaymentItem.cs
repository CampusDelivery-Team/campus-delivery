namespace CampusDelivery.Api.Models;

public sealed class SettlementPaymentItem
{
    public int SettlementId { get; set; }

    public int PaymentId { get; set; }

    public int RecordId { get; set; }

    public int TaskId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal PayAmount { get; set; }

    public string PayMethod { get; set; } = "CASH";
}

