namespace CampusDelivery.Api.Models;

public sealed class Settlement
{
    public int SettlementId { get; set; }

    public int RunnerId { get; set; }

    public string RunnerName { get; set; } = string.Empty;

    public decimal OrderTotal { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal NetIncome { get; set; }

    public string SettlementStatus { get; set; } = "WAITING";
}

