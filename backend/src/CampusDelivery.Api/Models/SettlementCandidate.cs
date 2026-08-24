namespace CampusDelivery.Api.Models;

public sealed class SettlementCandidate
{
    public int PaymentId { get; set; }

    public int RecordId { get; set; }

    public int TaskId { get; set; }

    public int RunnerId { get; set; }

    public string RunnerName { get; set; } = string.Empty;

    public string TaskTitle { get; set; } = string.Empty;

    public decimal OrderAmount { get; set; }

    public decimal PayAmount { get; set; }

    public string PayMethod { get; set; } = "CASH";

    public string PayStatus { get; set; } = "PAID";

    public string TaskStatus { get; set; } = "FINISHED";
}

