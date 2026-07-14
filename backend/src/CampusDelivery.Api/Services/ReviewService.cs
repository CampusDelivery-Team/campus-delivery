using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories;
using System;
using System.Collections.Generic;

namespace CampusDelivery.Api.Services;

public sealed class ReviewService
{
    private readonly ReviewsRepository _reviewsRepository;

    public ReviewService(ReviewsRepository reviewsRepository)
    {
        _reviewsRepository = reviewsRepository;
    }

    // 根据评价ID获取单条评价
    public Review? GetReviewById(int reviewId)
    {
        return _reviewsRepository.GetReviewById(reviewId);
    }

    // 根据报告ID获取该报告的所有评价（按时间倒序）
    public List<Review> GetReviewsByReportId(int reportId)
    {
        return _reviewsRepository.GetReviewsByReportId(reportId);
    }

    // 分页获取所有评价（管理员用）
    public List<Review> GetAllReviews(int pageNumber = 1, int pageSize = 20)
    {
        return _reviewsRepository.GetAllReviews(pageNumber, pageSize);
    }

    // 添加一条新评价（
    public (bool Success, string ErrorMessage) AddReview(Review review)
    {
        // 业务规则：若未指定评价时间，则设为当前时间
        if (review.Reviewed_at == default)
        {
            review.Reviewed_at = DateTime.Now;
        }

        // 可添加其他业务校验，例如 Rating 必须在 1~5 之间等（根据实际需求）
        if (review.Rating.HasValue && (review.Rating < 1 || review.Rating > 5))
        {
            return (false, "评分必须在 1 到 5 之间");
        }

        // 信用增量必须为整数，这里不做额外限制，由前端约束

        bool inserted = _reviewsRepository.InsertReview(review);
        return inserted ? (true, string.Empty) : (false, "保存评价失败，请稍后重试");
    }

    // 更新评价内容（仅允许修改评分、匿名标志、评论文本和信用增量）
    public (bool Success, string ErrorMessage) UpdateReview(Review review)
    {
        // 检查评价是否存在
        var existing = _reviewsRepository.GetReviewById(review.ReviewID);
        if (existing == null)
        {
            return (false, "要修改的评价不存在");
        }

        // 业务校验：评分范围
        if (review.Rating.HasValue && (review.Rating < 1 || review.Rating > 5))
        {
            return (false, "评分必须在 1 到 5 之间");
        }

        bool updated = _reviewsRepository.UpdateReview(review);
        return updated ? (true, string.Empty) : (false, "更新评价失败，请稍后重试");
    }

    //删除评论
    public (bool Success, string ErrorMessage) DeleteReview(int reviewId)
    {
        // 可选：先检查是否存在
        var existing = _reviewsRepository.GetReviewById(reviewId);
        if (existing == null)
        {
            return (false, "要删除的评价不存在");
        }

        bool deleted = _reviewsRepository.DeleteReview(reviewId);
        return deleted ? (true, string.Empty) : (false, "删除评价失败，请稍后重试");
    }

    // 获取评价总数
    public int GetTotalCount()
    {
        return _reviewsRepository.GetTotalCount();
    }
}
