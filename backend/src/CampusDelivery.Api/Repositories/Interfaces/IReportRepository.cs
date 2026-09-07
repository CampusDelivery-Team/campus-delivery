using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IReportRepository
{
    Task<IReadOnlyList<ReportMetricRecord>> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeVolumeRecord>> GetNodeVolumesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RunnerPerformanceRecord>> GetRunnerPerformanceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportRecord>> GetRecentReportsAsync(CancellationToken cancellationToken = default);
    Task<ReportRecord?> GetByIdAsync(int reportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportBusinessItem>> GetBusinessItemsAsync(string reportType, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetAuditIdsForReportAsync(string reportType, DateTime periodStart, DateTime periodEnd, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportAuditItem>> GetReportAuditItemsAsync(int reportId, CancellationToken cancellationToken = default);
    Task<int> InsertReportAsync(ReportRecord report, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task InsertReportAuditItemAsync(int reportId, int auditId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int reportId, string reportStatus, CancellationToken cancellationToken = default);
}
