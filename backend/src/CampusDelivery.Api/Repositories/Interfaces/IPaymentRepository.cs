using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentRecord?> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task<PaymentRecord?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<PaymentRecord?> GetByTaskIdWithLockAsync(int taskId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<PaymentRecord?> GetByIdWithLockAsync(int paymentId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentListRecord>> GetPaymentsByPublisherUserIdAsync(int publisherUserId, string? keyword, int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetPaymentsCountByPublisherUserIdAsync(int publisherUserId, string? keyword, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(PaymentRecord record, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdatePaymentAsync(int paymentId, PaymentRecord record, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdatePaymentStatusAsync(int paymentId, string payStatus, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
}
