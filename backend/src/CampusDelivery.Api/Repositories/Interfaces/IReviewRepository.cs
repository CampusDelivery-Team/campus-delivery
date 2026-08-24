using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default);
    Task<ReviewWriteContext?> GetWriteContextWithLockAsync(int reviewId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetAllPagedAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetByPublisherUserIdPagedAsync(int publisherUserId, int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByPublisherUserIdAsync(int publisherUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTaskIdAsync(int taskId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> InsertAsync(Review review, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Review review, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int reviewId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateRunnerCreditAsync(int runnerId, decimal creditDelta, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
}
