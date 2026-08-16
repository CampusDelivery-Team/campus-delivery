using System.Globalization;
using System.Text;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class ReportService(
    IReportRepository reportRepository,
    IRepositoryTransactionManager transactionManager) : IReportService
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
            RecentReports = recentReports.Select(ToReportRecordViewModel).ToList(),
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

        if (!TryParseMonth(model.StatPeriod, out DateTime periodStart, out DateTime periodEnd))
        {
            return new ReportOperationResult(false, "统计周期必须使用 yyyy-MM 格式，例如 2026-08。", null);
        }

        IReadOnlyList<ReportBusinessItem> businessItems = await reportRepository.GetBusinessItemsAsync(
            reportType,
            periodStart,
            periodEnd,
            cancellationToken);
        IReadOnlyList<ReportMetricViewModel> metrics = BuildMetrics(reportType, businessItems);

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            int reportId = await reportRepository.InsertReportAsync(new ReportRecord
            {
                ReportType = reportType,
                StatPeriod = periodStart.ToString("yyyy-MM"),
                ReportStatus = ReportStatusCodes.Generated
            }, transaction, cancellationToken);

            IReadOnlyList<int> auditIds = await reportRepository.GetAuditIdsForReportAsync(
                reportType,
                periodStart,
                periodEnd,
                transaction,
                cancellationToken);
            foreach (int auditId in auditIds)
            {
                await reportRepository.InsertReportAuditItemAsync(
                    reportId,
                    auditId,
                    transaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new ReportOperationResult(
                true,
                $"报表已生成，包含 {businessItems.Count} 条业务明细和 {auditIds.Count} 条审计依据。核心指标：{string.Join("，", metrics.Select(metric => $"{metric.Name} {metric.Value}"))}。",
                reportId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ReportDetailsViewModel?> GetDetailsAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        ReportRecord? report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report == null)
        {
            return null;
        }

        ResolveStoredPeriod(report, out DateTime periodStart, out DateTime periodEnd);
        IReadOnlyList<ReportBusinessItem> businessItems = await reportRepository.GetBusinessItemsAsync(
            report.ReportType,
            periodStart,
            periodEnd,
            cancellationToken);
        IReadOnlyList<ReportAuditItem> auditItems = await reportRepository.GetReportAuditItemsAsync(
            reportId,
            cancellationToken);

        return BuildDetailsViewModel(report, businessItems, auditItems);
    }

    public async Task<ReportExportResult> ExportAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        ReportRecord? report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report == null)
        {
            return new ReportExportResult(false, "报表不存在，无法导出。", null, null);
        }

        ResolveStoredPeriod(report, out DateTime periodStart, out DateTime periodEnd);
        IReadOnlyList<ReportBusinessItem> businessItems = await reportRepository.GetBusinessItemsAsync(
            report.ReportType,
            periodStart,
            periodEnd,
            cancellationToken);
        IReadOnlyList<ReportAuditItem> auditItems = await reportRepository.GetReportAuditItemsAsync(
            reportId,
            cancellationToken);
        ReportDetailsViewModel details = BuildDetailsViewModel(report, businessItems, auditItems);
        byte[] content = BuildCsv(details);

        if (!await reportRepository.UpdateStatusAsync(
                reportId,
                ReportStatusCodes.Exported,
                cancellationToken))
        {
            return new ReportExportResult(false, "导出文件已生成，但报表状态更新失败，请重试。", null, null);
        }

        string fileName = $"report-{report.ReportType.ToLowerInvariant()}-{report.StatPeriod}-{report.ReportId}.csv";
        return new ReportExportResult(true, string.Empty, fileName, content);
    }

    private static ReportDetailsViewModel BuildDetailsViewModel(
        ReportRecord report,
        IReadOnlyList<ReportBusinessItem> businessItems,
        IReadOnlyList<ReportAuditItem> auditItems)
    {
        return new ReportDetailsViewModel
        {
            Report = ToReportRecordViewModel(report),
            PeriodBasisNote = GetPeriodBasisNote(report.ReportType),
            Metrics = BuildMetrics(report.ReportType, businessItems),
            BusinessItems = businessItems.Select(item => new ReportBusinessItemViewModel
            {
                BusinessId = item.BusinessId,
                TaskId = item.TaskId,
                TaskTitle = item.TaskTitle,
                StatusDisplayName = GetBusinessStatusDisplayName(report.ReportType, item.PrimaryStatus),
                RelatedStatusDisplayName = string.IsNullOrEmpty(item.SecondaryStatus)
                    ? null
                    : DisplayNameService.GetRefundStatusName(item.SecondaryStatus),
                Amount = item.Amount,
                OccurredAt = item.OccurredAt,
                Description = item.Description
            }).ToList(),
            AuditItems = auditItems.Select(item => new ReportAuditItemViewModel
            {
                AuditId = item.AuditId,
                AuditObjectDisplayName = DisplayNameService.GetAuditObjectName(item.AuditObject),
                AuditResultDisplayName = DisplayNameService.GetAuditResultName(item.AuditResult),
                AuditedAt = item.AuditedAt,
                ExceptionNote = item.ExceptionNote
            }).ToList()
        };
    }

    private static IReadOnlyList<ReportMetricViewModel> BuildMetrics(
        string reportType,
        IReadOnlyList<ReportBusinessItem> items)
    {
        List<ReportMetricRecord> metrics = reportType switch
        {
            "ORDER" =>
            [
                Metric("任务数量", items.Count, "统计周期内创建的任务"),
                Metric("已完成", items.Count(item => item.PrimaryStatus == TaskStatusCodes.Finished), "完成配送并进入完成状态"),
                Metric("取消/退款中", items.Count(item => item.PrimaryStatus is TaskStatusCodes.Cancelled or TaskStatusCodes.Refunding), "异常或未闭环任务"),
                MoneyMetric("任务金额", items.Sum(item => item.Amount), "任务标价合计")
            ],
            "PAYMENT" =>
            [
                Metric("支付记录", items.Count, "统计周期内关联任务的支付记录"),
                Metric("已支付", items.Count(item => item.PrimaryStatus == PaymentStatusCodes.Paid && !RefundStatusCodes.IsActive(item.SecondaryStatus ?? string.Empty)), "已支付且不在退款处理中"),
                Metric("待付款", items.Count(item => item.PrimaryStatus == PaymentStatusCodes.Unpaid), "已保存但尚未付款"),
                Metric("退款/退款中", items.Count(item => item.PrimaryStatus == PaymentStatusCodes.Refunded || RefundStatusCodes.IsActive(item.SecondaryStatus ?? string.Empty)), "已退款或存在活动退款"),
                MoneyMetric("有效支付金额", items.Where(item => item.PrimaryStatus == PaymentStatusCodes.Paid && !RefundStatusCodes.IsActive(item.SecondaryStatus ?? string.Empty)).Sum(item => item.Amount), "排除退款及退款中记录")
            ],
            _ =>
            [
                Metric("投诉数量", items.Count, "统计周期内关联任务的投诉"),
                Metric("处理中", items.Count(item => item.PrimaryStatus is ComplaintStatusCodes.Submitted or ComplaintStatusCodes.Processing), "尚未完成处理"),
                Metric("已处理", items.Count(item => item.PrimaryStatus == ComplaintStatusCodes.Done), "处理状态为已完成")
            ]
        };

        return metrics.Select(ReportMetricViewModel.FromModel).ToList();
    }

    private static ReportMetricRecord Metric(string name, int value, string note) => new()
    {
        Name = name,
        Value = value.ToString("N0"),
        Note = note
    };

    private static ReportMetricRecord MoneyMetric(string name, decimal value, string note) => new()
    {
        Name = name,
        Value = $"¥{value:F2}",
        Note = note
    };

    private static string GetBusinessStatusDisplayName(string reportType, string status) => reportType switch
    {
        "ORDER" => DisplayNameService.GetTaskStatusName(status),
        "PAYMENT" => DisplayNameService.GetPayStatusName(status),
        _ => DisplayNameService.GetComplaintStatusName(status)
    };

    private static string GetPeriodBasisNote(string reportType) => reportType switch
    {
        "ORDER" => "订单报表按 tasks.created_at（任务创建时间）归入统计月份。",
        "PAYMENT" => "现有 payments 表没有支付时间字段，支付报表按任务 completed_at；未记录完成时间时回退到 created_at。",
        _ => "现有 complaints 表没有提交时间字段，投诉报表按关联任务 created_at 归入统计月份。"
    };

    private static byte[] BuildCsv(ReportDetailsViewModel details)
    {
        var builder = new StringBuilder();
        builder.AppendLine("报表编号,报表类型,统计周期,状态,生成时间");
        builder.AppendLine(string.Join(',',
            EscapeCsv(details.Report.ReportId.ToString()),
            EscapeCsv(details.Report.ReportTypeDisplayName),
            EscapeCsv(details.Report.StatPeriod),
            EscapeCsv(details.Report.ReportStatusDisplayName),
            EscapeCsv(details.Report.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"))));
        builder.AppendLine();
        builder.AppendLine("指标,值,说明");
        foreach (ReportMetricViewModel metric in details.Metrics)
        {
            builder.AppendLine(string.Join(',', EscapeCsv(metric.Name), EscapeCsv(metric.Value), EscapeCsv(metric.Note)));
        }

        builder.AppendLine();
        builder.AppendLine("业务编号,任务编号,任务标题,状态,关联状态,金额,统计时间,说明");
        foreach (ReportBusinessItemViewModel item in details.BusinessItems)
        {
            builder.AppendLine(string.Join(',',
                EscapeCsv(item.BusinessId.ToString()),
                EscapeCsv(item.TaskId.ToString()),
                EscapeCsv(item.TaskTitle),
                EscapeCsv(item.StatusDisplayName),
                EscapeCsv(item.RelatedStatusDisplayName),
                EscapeCsv(item.Amount.ToString("F2")),
                EscapeCsv(item.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(item.Description)));
        }

        builder.AppendLine();
        builder.AppendLine("审计编号,审计对象,审计结果,审计时间,异常说明");
        foreach (ReportAuditItemViewModel item in details.AuditItems)
        {
            builder.AppendLine(string.Join(',',
                EscapeCsv(item.AuditId.ToString()),
                EscapeCsv(item.AuditObjectDisplayName),
                EscapeCsv(item.AuditResultDisplayName),
                EscapeCsv(item.AuditedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(item.ExceptionNote)));
        }

        byte[] body = Encoding.UTF8.GetBytes(builder.ToString());
        return Encoding.UTF8.GetPreamble().Concat(body).ToArray();
    }

    private static string EscapeCsv(string? value)
    {
        string text = value ?? string.Empty;
        if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@')
        {
            text = "'" + text;
        }

        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static bool TryParseMonth(string? statPeriod, out DateTime periodStart, out DateTime periodEnd)
    {
        bool valid = DateTime.TryParseExact(
            statPeriod?.Trim(),
            "yyyy-MM",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out periodStart);
        periodEnd = valid ? periodStart.AddMonths(1) : default;
        return valid;
    }

    private static void ResolveStoredPeriod(
        ReportRecord report,
        out DateTime periodStart,
        out DateTime periodEnd)
    {
        if (TryParseMonth(report.StatPeriod, out periodStart, out periodEnd))
        {
            return;
        }

        periodStart = new DateTime(report.GeneratedAt.Year, report.GeneratedAt.Month, 1);
        periodEnd = periodStart.AddMonths(1);
    }

    private static ReportRecordViewModel ToReportRecordViewModel(ReportRecord record) =>
        ReportRecordViewModel.FromModel(
            record,
            DisplayNameService.GetReportTypeName(record.ReportType),
            DisplayNameService.GetReportStatusName(record.ReportStatus));

    private static string NormalizeReportType(string? reportType) =>
        string.IsNullOrWhiteSpace(reportType) ? "ORDER" : reportType.Trim().ToUpperInvariant();
}
