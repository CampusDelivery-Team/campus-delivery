namespace CampusDelivery.Api.Models;

public sealed class ReportBusinessItem
{
    public int BusinessId { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string PrimaryStatus { get; set; } = string.Empty;
    public string? SecondaryStatus { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class ReportAuditItem
{
    public int AuditId { get; set; }
    public string AuditObject { get; set; } = string.Empty;
    public string AuditResult { get; set; } = string.Empty;
    public DateTime AuditedAt { get; set; }
    public string? ExceptionNote { get; set; }
}
