using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IReportService
{
    Task<ReportDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<ReportOperationResult> GenerateAsync(ReportGenerateViewModel model, CancellationToken cancellationToken = default);
    Task<ReportDetailsViewModel?> GetDetailsAsync(int reportId, CancellationToken cancellationToken = default);
    Task<ReportExportResult> ExportAsync(int reportId, CancellationToken cancellationToken = default);
    Task<ReportOperationResult> DeleteAsync(int reportId, CancellationToken cancellationToken = default);
}

public sealed record ReportExportResult(
    bool Success,
    string Message,
    string? FileName,
    byte[]? Content);
