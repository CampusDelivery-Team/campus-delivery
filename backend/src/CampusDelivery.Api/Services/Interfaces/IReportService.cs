using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IReportService
{
    Task<ReportDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<ReportOperationResult> GenerateAsync(ReportGenerateViewModel model, CancellationToken cancellationToken = default);
}
