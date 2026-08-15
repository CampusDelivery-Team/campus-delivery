using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IReportRepository
{
    Task<IReadOnlyList<ReportMetricRecord>> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeVolumeRecord>> GetNodeVolumesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RunnerPerformanceRecord>> GetRunnerPerformanceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportRecord>> GetRecentReportsAsync(CancellationToken cancellationToken = default);
    Task<int> InsertReportAsync(ReportRecord report, CancellationToken cancellationToken = default);
}
