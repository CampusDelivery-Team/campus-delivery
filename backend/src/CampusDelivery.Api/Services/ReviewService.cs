using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;

namespace CampusDelivery.Api.Services;

public sealed class ReviewService(ReviewsRepository repository)
{
    public bool CanReview(int recordId, int publisherUserId) =>
        repository.CanReview(recordId, publisherUserId);

    public ReviewListItemViewModel? GetByRecordId(int recordId) =>
        Map(repository.GetReviewByRecordId(recordId));

    public ReviewEditViewModel? GetEditModel(int reviewId)
    {
        Review? review = repository.GetReviewById(reviewId);
        return review == null
            ? null
            : new ReviewEditViewModel
            {
                ReviewId = review.ReviewId,
                RecordId = review.RecordId,
                Rating = review.Rating,
                AnonymousFlag = review.AnonymousFlag,
                CommentText = review.CommentText
            };
    }

    public IReadOnlyList<ReviewListItemViewModel> GetAll(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        return repository.GetAllReviews(page, pageSize).Select(review => Map(review)!).ToList();
    }

    public int GetTotalCount() => repository.GetTotalCount();

    public (bool Success, string ErrorMessage) Add(ReviewCreateViewModel model, int publisherUserId)
    {
        if (!repository.CanReview(model.RecordId, publisherUserId))
        {
            return (false, "只有任务发布者可以评价已完成且尚未评价的任务。");
        }

        Review review = new Review
        {
            RecordId = model.RecordId,
            Rating = model.Rating,
            AnonymousFlag = model.AnonymousFlag,
            CommentText = model.CommentText?.Trim()
        };
        return repository.InsertReview(review, publisherUserId)
            ? (true, string.Empty)
            : (false, "评价提交失败，任务状态可能已经变化。");
    }

    public (bool Success, string ErrorMessage) Update(ReviewEditViewModel model)
    {
        Review? existing = repository.GetReviewById(model.ReviewId);
        if (existing == null)
        {
            return (false, "评价不存在。");
        }

        existing.Rating = model.Rating;
        existing.AnonymousFlag = model.AnonymousFlag;
        existing.CommentText = model.CommentText?.Trim();
        return repository.UpdateReview(existing)
            ? (true, string.Empty)
            : (false, "评价更新失败。");
    }

    public (bool Success, string ErrorMessage) Delete(int reviewId) =>
        repository.DeleteReview(reviewId)
            ? (true, string.Empty)
            : (false, "评价不存在或已被删除。");

    private static ReviewListItemViewModel? Map(Review? review)
    {
        if (review == null)
        {
            return null;
        }

        return new ReviewListItemViewModel
        {
            ReviewId = review.ReviewId,
            RecordId = review.RecordId,
            Rating = review.Rating,
            AnonymousDisplayName = review.AnonymousFlag == "Y" ? "匿名" : "实名",
            CommentText = review.CommentText,
            ReviewedAt = review.ReviewedAt,
            CreditDelta = review.CreditDelta
        };
    }
}
