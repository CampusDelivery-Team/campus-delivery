using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IRefundRepository
{
    Task<RefundRecord?> GetByIdAsync(int refundId, CancellationToken cancellationToken = default);
    Task<RefundRecord?> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<RefundRecord?> GetActiveByPaymentIdWithLockAsync(int paymentId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<RefundRecord?> GetByIdWithLockAsync(int refundId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefundListRecord>> GetRefundsAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetRefundsCountAsync(CancellationToken cancellationToken = default);
    Task<int> InsertAsync(RefundRecord record, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateReviewAsync(int refundId, string processStatus, decimal approvedAmount, string combinedReason, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
}
