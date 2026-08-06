namespace CampusDelivery.Api.Models;

public sealed class ReportRecord
{
    public int ReportId { get; set; }

    public string ReportType { get; set; } = "ORDER";

    public string StatPeriod { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public string ReportStatus { get; set; } = "GENERATED";
}

