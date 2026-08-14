using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;

namespace CampusDelivery.Api.Services;

public sealed class ReportService(ReportRepository reportRepository)
{
    public async Task<ReportDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReportMetricRecord> metrics = await reportRepository.GetMetricsAsync(cancellationToken);
        IReadOnlyList<NodeVolumeRecord> nodeVolumes = await reportRepository.GetNodeVolumesAsync(cancellationToken);
        IReadOnlyList<RunnerPerformanceRecord> runnerPerformances = await reportRepository.GetRunnerPerformanceAsync(cancellationToken);
        IReadOnlyList<ReportRecord> recentReports = await reportRepository.GetRecentReportsAsync(cancellationToken);

        return new ReportDashboardViewModel
        {
            Metrics = metrics.Select(ReportMetricViewModel.FromModel).ToList(),
            NodeVolumes = nodeVolumes.Select(NodeVolumeViewModel.FromModel).ToList(),
            RunnerPerformances = runnerPerformances.Select(RunnerPerformanceViewModel.FromModel).ToList(),
            RecentReports = recentReports.Select(ReportRecordViewModel.FromModel).ToList(),
            GenerateForm = new ReportGenerateViewModel
            {
                ReportType = "ORDER",
                StatPeriod = DateTime.Now.ToString("yyyy-MM")
            }
        };
    }

    public async Task<ReportOperationResult> GenerateAsync(
        ReportGenerateViewModel model,
        CancellationToken cancellationToken = default)
    {
        string reportType = NormalizeReportType(model.ReportType);
        if (reportType is not ("ORDER" or "PAYMENT" or "COMPLAINT"))
        {
            return new ReportOperationResult(false, "报表类型无效。", null);
        }

        string statPeriod = string.IsNullOrWhiteSpace(model.StatPeriod)
            ? DateTime.Now.ToString("yyyy-MM")
            : model.StatPeriod.Trim();
        if (statPeriod.Length > 50)
        {
            return new ReportOperationResult(false, "统计周期不能超过50个字符。", null);
        }

        int reportId = await reportRepository.InsertReportAsync(new ReportRecord
        {
            ReportType = reportType,
            StatPeriod = statPeriod,
            ReportStatus = "GENERATED"
        }, cancellationToken);

        return new ReportOperationResult(true, "报表生成记录已保存。", reportId);
    }

    private static string NormalizeReportType(string? reportType)
    {
        return string.IsNullOrWhiteSpace(reportType) ? "ORDER" : reportType.Trim().ToUpperInvariant();
    }
}

public sealed record ReportOperationResult(bool Success, string Message, int? ReportId);
