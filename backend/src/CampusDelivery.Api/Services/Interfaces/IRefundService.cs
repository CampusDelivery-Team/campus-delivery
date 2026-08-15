using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IRefundService
{
    Task<RefundCreateViewModel?> BuildCreateModelAsync(int taskId, int currentUserId, CancellationToken cancellationToken = default);
    Task<RefundOperationResult> SubmitAsync(RefundCreateViewModel model, int currentUserId, CancellationToken cancellationToken = default);
    Task<RefundAdminListViewModel> GetAdminListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RefundReviewViewModel?> GetReviewModelAsync(int refundId, CancellationToken cancellationToken = default);
    Task<RefundOperationResult> ReviewAsync(int refundId, string decision, string reviewReason, int adminUserId, CancellationToken cancellationToken = default);
}
