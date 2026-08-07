using System.ComponentModel.DataAnnotations;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReportDashboardViewModel
{
    public IReadOnlyList<ReportMetricViewModel> Metrics { get; set; } = Array.Empty<ReportMetricViewModel>();

    public IReadOnlyList<NodeVolumeViewModel> NodeVolumes { get; set; } = Array.Empty<NodeVolumeViewModel>();

    public IReadOnlyList<RunnerPerformanceViewModel> RunnerPerformances { get; set; } = Array.Empty<RunnerPerformanceViewModel>();

    public IReadOnlyList<ReportRecordViewModel> RecentReports { get; set; } = Array.Empty<ReportRecordViewModel>();

    public ReportGenerateViewModel GenerateForm { get; set; } = new();
}

public sealed class ReportGenerateViewModel
{
    [Required]
    public string ReportType { get; set; } = "ORDER";

    [Required]
    [StringLength(50)]
    public string StatPeriod { get; set; } = DateTime.Now.ToString("yyyy-MM");
}

public sealed class ReportMetricViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public static ReportMetricViewModel FromModel(ReportMetricRecord record)
    {
        return new ReportMetricViewModel
        {
            Name = record.Name,
            Value = record.Value,
            Note = record.Note
        };
    }
}

public sealed class NodeVolumeViewModel
{
    public int NodeId { get; set; }

    public string NodeName { get; set; } = string.Empty;

    public int TaskCount { get; set; }

    public int FinishedTaskCount { get; set; }

    public static NodeVolumeViewModel FromModel(NodeVolumeRecord record)
    {
        return new NodeVolumeViewModel
        {
            NodeId = record.NodeId,
            NodeName = record.NodeName,
            TaskCount = record.TaskCount,
            FinishedTaskCount = record.FinishedTaskCount
        };
    }
}

public sealed class RunnerPerformanceViewModel
{
    public int RunnerId { get; set; }

    public string RunnerName { get; set; } = string.Empty;

    public int FinishedTaskCount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal SettledIncome { get; set; }

    public static RunnerPerformanceViewModel FromModel(RunnerPerformanceRecord record)
    {
        return new RunnerPerformanceViewModel
        {
            RunnerId = record.RunnerId,
            RunnerName = record.RunnerName,
            FinishedTaskCount = record.FinishedTaskCount,
            PaidAmount = record.PaidAmount,
            SettledIncome = record.SettledIncome
        };
    }
}

public sealed class ReportRecordViewModel
{
    public int ReportId { get; set; }

    public string ReportType { get; set; } = "ORDER";

    public string StatPeriod { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public string ReportStatus { get; set; } = "GENERATED";

    public string ReportTypeDisplayName => DisplayNameService.GetReportTypeName(ReportType);

    public string ReportStatusDisplayName => DisplayNameService.GetReportStatusName(ReportStatus);

    public static ReportRecordViewModel FromModel(ReportRecord record)
    {
        return new ReportRecordViewModel
        {
            ReportId = record.ReportId,
            ReportType = record.ReportType,
            StatPeriod = record.StatPeriod,
            GeneratedAt = record.GeneratedAt,
            ReportStatus = record.ReportStatus
        };
    }
}

