namespace CampusDelivery.Api.Models;

public sealed class RunnerPerformanceRecord
{
    public int RunnerId { get; set; }

    public string RunnerName { get; set; } = string.Empty;

    public int FinishedTaskCount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal SettledIncome { get; set; }
}

