using System.ComponentModel.DataAnnotations;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class AuditIndexViewModel
{
    public IReadOnlyList<AuditLogItemViewModel> Logs { get; set; } = Array.Empty<AuditLogItemViewModel>();

    public int PaymentTargetCount { get; set; }

    public int RefundTargetCount { get; set; }

    public int StatusLogTargetCount { get; set; }
}

public sealed class AuditCreateViewModel
{
    [Required]
    public string AuditObject { get; set; } = "PAYMENT";

    [Required]
    public string AuditResult { get; set; } = "PASS";

    [StringLength(300)]
    public string? ExceptionNote { get; set; }

    public List<int> TargetIds { get; set; } = new();

    public IReadOnlyList<AuditTargetViewModel> Targets { get; set; } = Array.Empty<AuditTargetViewModel>();

    public string AuditObjectDisplayName => DisplayNameService.GetAuditObjectName(AuditObject);
}

public sealed class AuditLogItemViewModel
{
    public int AuditId { get; set; }

    public string AuditObject { get; set; } = "PAYMENT";

    public string AuditResult { get; set; } = "PASS";

    public DateTime AuditedAt { get; set; }

    public string? ExceptionNote { get; set; }

    public int RelatedCount { get; set; }

    public string AuditObjectDisplayName => DisplayNameService.GetAuditObjectName(AuditObject);

    public string AuditResultDisplayName => DisplayNameService.GetAuditResultName(AuditResult);

    public static AuditLogItemViewModel FromModel(AuditLogRecord record)
    {
        return new AuditLogItemViewModel
        {
            AuditId = record.AuditId,
            AuditObject = record.AuditObject,
            AuditResult = record.AuditResult,
            AuditedAt = record.AuditedAt,
            ExceptionNote = record.ExceptionNote,
            RelatedCount = record.RelatedCount
        };
    }
}

public sealed class AuditTargetViewModel
{
    public int TargetId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string SecondaryText { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? OccurredAt { get; set; }

    public static AuditTargetViewModel FromModel(AuditTargetRecord record)
    {
        return new AuditTargetViewModel
        {
            TargetId = record.TargetId,
            Title = record.Title,
            SecondaryText = record.SecondaryText,
            Amount = record.Amount,
            Status = record.Status,
            OccurredAt = record.OccurredAt
        };
    }
}

