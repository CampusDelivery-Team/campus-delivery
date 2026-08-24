namespace CampusDelivery.Api.Models;

public sealed class RunnerSettlementSummary
{
    public int SettlementCount { get; set; }

    public int WaitingCount { get; set; }

    public int DoneCount { get; set; }

    public int BlockedCount { get; set; }

    public decimal TotalNetIncome { get; set; }

    public decimal WaitingNetIncome { get; set; }

    public decimal DoneNetIncome { get; set; }
}
