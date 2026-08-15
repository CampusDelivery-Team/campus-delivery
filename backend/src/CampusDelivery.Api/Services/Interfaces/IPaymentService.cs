using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentConfirmViewModel?> BuildConfirmModelAsync(int taskId, int currentUserId, CancellationToken cancellationToken = default);
    Task<PaymentOperationResult> SubmitPaymentAsync(int taskId, int currentUserId, string payMethod, CancellationToken cancellationToken = default);
    Task<PaymentOperationResult> SaveUnpaidPaymentAsync(int taskId, int currentUserId, string payMethod, CancellationToken cancellationToken = default);
    Task<PaymentStatusQueryViewModel> GetMyPaymentStatusAsync(int currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PaymentStatusQueryViewModel> QueryPaymentAsync(int currentUserId, int? taskId, int? paymentId, CancellationToken cancellationToken = default);
}
