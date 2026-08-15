using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IReviewService
{
    Task<IReadOnlyList<Review>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task<Review?> GetEditableReviewAsync(int reviewId, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetMyReviewsAsync(int currentUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateReviewAsync(int taskId, int rating, char anonymousFlag, string? commentText, int currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateReviewAsync(int reviewId, int rating, char anonymousFlag, string? commentText, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
