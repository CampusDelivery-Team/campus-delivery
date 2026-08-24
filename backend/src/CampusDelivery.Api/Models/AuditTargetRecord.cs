namespace CampusDelivery.Api.Models;

public sealed class AuditTargetRecord
{
    public int TargetId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string SecondaryText { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? OccurredAt { get; set; }
}

