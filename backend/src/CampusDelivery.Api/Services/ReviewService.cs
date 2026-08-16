using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class ReviewService(
    IReviewRepository reviewsRepository,
    IAssignRepository assignRepository,
    IPaymentRepository paymentRepository,
    IRefundRepository refundRepository,
    IRepositoryTransactionManager transactionManager) : IReviewService
{
    public async Task<IReadOnlyList<Review>> GetByTaskIdAsync(
        int taskId,
        CancellationToken cancellationToken = default) =>
        await reviewsRepository.GetByTaskIdAsync(taskId, cancellationToken);

    public async Task<Review?> GetEditableReviewAsync(
        int reviewId,
        int currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        Review? review = await reviewsRepository.GetByIdAsync(reviewId, cancellationToken);
        return review != null && CanManageReview(review.PublisherUserId, currentUserId, isAdmin)
            ? review
            : null;
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize, 20);
        int total = await reviewsRepository.GetTotalCountAsync(cancellationToken);
        page = ClampPage(page, total, pageSize);
        int offset = (page - 1) * pageSize;
        IReadOnlyList<Review> items = await reviewsRepository.GetAllPagedAsync(
            offset,
            pageSize,
            cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetMyReviewsAsync(
        int currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize, 10);
        int total = await reviewsRepository.GetCountByPublisherUserIdAsync(
            currentUserId,
            cancellationToken);
        page = ClampPage(page, total, pageSize);
        int offset = (page - 1) * pageSize;
        IReadOnlyList<Review> items = await reviewsRepository.GetByPublisherUserIdPagedAsync(
            currentUserId,
            offset,
            pageSize,
            cancellationToken);
        return (items, total);
    }

    public async Task<(bool Success, string Message)> CreateReviewAsync(
        int taskId,
        int rating,
        char anonymousFlag,
        string? commentText,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        string? validationError = ValidateInput(rating, anonymousFlag, commentText);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            string? taskStatus = await assignRepository.GetTaskStatusWithLockAsync(
                taskId,
                transaction,
                cancellationToken);
            if (taskStatus == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "任务不存在，无法提交评价");
            }

            int? publisherUserId = await assignRepository.GetTaskPublisherUserIdAsync(
                taskId,
                transaction,
                cancellationToken);
            if (publisherUserId != currentUserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "只能评价自己发布的任务");
            }

            if (!string.Equals(taskStatus, TaskStatusCodes.Finished, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "只有已完成的任务才能评价");
            }

            AssignRecord? assignRecord = await assignRepository.GetLatestAssignRecordWithLockAsync(
                taskId,
                transaction,
                cancellationToken);
            if (assignRecord == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "该任务没有有效的接派记录，暂时无法评价");
            }

            PaymentRecord? payment = await paymentRepository.GetByTaskIdWithLockAsync(
                taskId,
                transaction,
                cancellationToken);
            if (payment == null || payment.RecordId != assignRecord.RecordId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "该任务没有与最终接派记录匹配的有效支付，暂时无法评价");
            }

            if (!string.Equals(payment.PayStatus, PaymentStatusCodes.Paid, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "任务完成付款后才能评价，待付款、失败或已退款订单不能评价");
            }

            RefundRecord? activeRefund = await refundRepository.GetActiveByPaymentIdWithLockAsync(
                payment.PaymentId,
                transaction,
                cancellationToken);
            if (activeRefund != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "该订单正在退款处理中，暂时不能评价");
            }

            Runner? runner = await assignRepository.GetRunnerWithLockAsync(
                assignRecord.RunnerId,
                transaction,
                cancellationToken);
            if (runner == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "该任务没有有效的跑腿员，暂时无法评价");
            }

            if (await reviewsRepository.ExistsByTaskIdAsync(
                    taskId,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "该任务已经评价过，不能重复评价");
            }

            decimal creditDelta = CalculateAppliedCreditDelta(
                runner.CreditScore,
                CalculateCreditDelta(rating));
            var review = new Review
            {
                TaskId = taskId,
                RecordId = assignRecord.RecordId,
                PublisherUserId = currentUserId,
                Rating = rating,
                AnonymousFlag = anonymousFlag,
                CommentText = NormalizeComment(commentText),
                ReviewedAt = DateTime.Now,
                CreditDelta = creditDelta
            };

            if (!await reviewsRepository.InsertAsync(
                    review,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "评价保存失败，请稍后重试");
            }

            if (creditDelta != 0 && !await reviewsRepository.UpdateRunnerCreditAsync(
                    runner.RunnerId,
                    creditDelta,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "跑腿员信誉分更新失败，评价未保存");
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "评价提交成功");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateReviewAsync(
        int reviewId,
        int rating,
        char anonymousFlag,
        string? commentText,
        int currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        string? validationError = ValidateInput(rating, anonymousFlag, commentText);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            ReviewWriteContext? context = await reviewsRepository.GetWriteContextWithLockAsync(
                reviewId,
                transaction,
                cancellationToken);
            if (context == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "评价不存在");
            }

            Review existing = context.Review;
            if (!CanManageReview(existing.PublisherUserId, currentUserId, isAdmin))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "只能修改自己提交的评价");
            }

            Runner? runner = await assignRepository.GetRunnerWithLockAsync(
                context.RunnerId,
                transaction,
                cancellationToken);
            if (runner == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "关联的跑腿员不存在，评价未修改");
            }

            decimal requestedDifference = CalculateCreditDelta(rating) - existing.CreditDelta;
            decimal creditDifference = CalculateAppliedCreditDelta(
                runner.CreditScore,
                requestedDifference);
            decimal newCreditDelta = existing.CreditDelta + creditDifference;
            existing.Rating = rating;
            existing.AnonymousFlag = anonymousFlag;
            existing.CommentText = NormalizeComment(commentText);
            existing.CreditDelta = newCreditDelta;

            if (!await reviewsRepository.UpdateAsync(
                    existing,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "评价更新失败，请稍后重试");
            }

            if (creditDifference != 0 && !await reviewsRepository.UpdateRunnerCreditAsync(
                    runner.RunnerId,
                    creditDifference,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "跑腿员信誉分更新失败，评价未修改");
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "评价更新成功");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> DeleteReviewAsync(
        int reviewId,
        int currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            ReviewWriteContext? context = await reviewsRepository.GetWriteContextWithLockAsync(
                reviewId,
                transaction,
                cancellationToken);
            if (context == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "评价不存在");
            }

            Review existing = context.Review;
            if (!CanManageReview(existing.PublisherUserId, currentUserId, isAdmin))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "只能删除自己提交的评价");
            }

            Runner? runner = await assignRepository.GetRunnerWithLockAsync(
                context.RunnerId,
                transaction,
                cancellationToken);
            if (runner == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "关联的跑腿员不存在，评价未删除");
            }

            if (!await reviewsRepository.DeleteAsync(
                    reviewId,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "评价删除失败，请稍后重试");
            }

            decimal rollbackCredit = CalculateAppliedCreditDelta(
                runner.CreditScore,
                -existing.CreditDelta);
            if (rollbackCredit != 0 && !await reviewsRepository.UpdateRunnerCreditAsync(
                    runner.RunnerId,
                    rollbackCredit,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "跑腿员信誉分回滚失败，评价未删除");
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "评价已删除，相关信誉分变化已撤销");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static int CalculateCreditDelta(int rating) => rating switch
    {
        5 => 2,
        4 => 1,
        3 => 0,
        2 => -1,
        1 => -2,
        _ => throw new ArgumentOutOfRangeException(nameof(rating))
    };

    private static decimal CalculateAppliedCreditDelta(
        decimal currentCreditScore,
        decimal requestedDelta)
    {
        decimal updatedCreditScore = Math.Max(0m, currentCreditScore + requestedDelta);
        return updatedCreditScore - currentCreditScore;
    }

    private static string? ValidateInput(int rating, char anonymousFlag, string? commentText)
    {
        if (rating is < 1 or > 5)
        {
            return "评分必须在1到5之间";
        }

        if (anonymousFlag is not ('Y' or 'N'))
        {
            return "匿名选项无效";
        }

        if (commentText?.Trim().Length > 300)
        {
            return "评价内容不能超过300字";
        }

        return null;
    }

    private static string? NormalizeComment(string? commentText) =>
        string.IsNullOrWhiteSpace(commentText) ? null : commentText.Trim();

    private static bool CanManageReview(int publisherUserId, int currentUserId, bool isAdmin) =>
        isAdmin || publisherUserId == currentUserId;

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize, int defaultPageSize)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 50 ? pageSize : defaultPageSize;
        return (page, pageSize);
    }

    private static int ClampPage(int page, int totalCount, int pageSize)
    {
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        return Math.Min(page, totalPages);
    }
}
