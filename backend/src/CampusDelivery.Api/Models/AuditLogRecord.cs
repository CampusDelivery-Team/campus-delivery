namespace CampusDelivery.Api.Models;

public sealed class AuditLogRecord
{
    public int AuditId { get; set; }

    public string AuditObject { get; set; } = "PAYMENT";

    public string AuditResult { get; set; } = "PASS";

    public DateTime AuditedAt { get; set; }

    public string? ExceptionNote { get; set; }

    public int RelatedCount { get; set; }
}

