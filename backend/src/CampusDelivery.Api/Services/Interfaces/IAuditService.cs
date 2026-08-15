using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IAuditService
{
    Task<AuditIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<AuditCreateViewModel> BuildCreateModelAsync(string auditObject, CancellationToken cancellationToken = default);
    Task<AuditOperationResult> CreateAuditAsync(AuditCreateViewModel model, CancellationToken cancellationToken = default);
}
