using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class ReviewService(
    ReviewsRepository reviewsRepository,
    OracleConnectionFactory connectionFactory)
{
    public async Task<Review?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default)
        => await reviewsRepository.GetByIdAsync(reviewId, cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
        => await reviewsRepository.GetByTaskIdAsync(taskId, cancellationToken);

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 50 ? pageSize : 20;
        int total = await reviewsRepository.GetTotalCountAsync(cancellationToken);
        int offset = (page - 1) * pageSize;
        var items = await reviewsRepository.GetAllPagedAsync(offset, pageSize, cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetMyPagedAsync(
        int publisherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 50 ? pageSize : 10;
        int total = await reviewsRepository.GetCountByPublisherUserIdAsync(publisherUserId, cancellationToken);
        int offset = (page - 1) * pageSize;
        var items = await reviewsRepository.GetByPublisherUserIdAsync(
            publisherUserId, offset, pageSize, cancellationToken);
        return (items, total);
    }

    public async Task<bool> CanCreateReviewAsync(
        int recordId, int publisherUserId, CancellationToken cancellationToken = default)
        => await reviewsRepository.CanCreateAsync(recordId, publisherUserId, cancellationToken);

    public async Task<(bool Success, string Message)> CreateReviewAsync(
        int recordId, int rating, char anonymousFlag, string? commentText, int publisherUserId,
        CancellationToken cancellationToken = default)
    {
        if (rating < 1 || rating > 5) return (false, "评分必须在1到5之间");
        if (commentText?.Length > 300) return (false, "评价内容不能超过300字");
        anonymousFlag = anonymousFlag == 'Y' ? 'Y' : 'N';
        commentText = string.IsNullOrWhiteSpace(commentText) ? null : commentText.Trim();
        await using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = (OracleTransaction)(await conn.BeginTransactionAsync(cancellationToken));
        try
        {
            int? runnerId = await reviewsRepository.GetEligibleRunnerIdAsync(
                recordId, publisherUserId, conn, tx, cancellationToken);
            if (!runnerId.HasValue)
            {
                await tx.RollbackAsync(cancellationToken);
                return (false, "只能评价自己已完成且尚未评价的订单");
            }

            var existing = await reviewsRepository.GetByRecordIdAsync(recordId, conn, tx, cancellationToken);
            if (existing != null)
            {
                await tx.RollbackAsync(cancellationToken);
                return (false, "该接派记录已有评价");
            }

            int creditDelta = CalculateCreditDelta(rating);
            var rv = new Review { RecordId = recordId, Rating = rating, AnonymousFlag = anonymousFlag, CommentText = commentText, ReviewedAt = DateTime.Now, CreditDelta = creditDelta };
            if (!await reviewsRepository.InsertAsync(rv, conn, tx, cancellationToken))
            { await tx.RollbackAsync(cancellationToken); return (false, "保存评价失败"); }
            if (creditDelta != 0)
                await UpdateRunnerCreditAsync(runnerId.Value, creditDelta, conn, tx, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return (true, "评价提交成功");
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<(bool Success, string Message)> UpdateReviewAsync(
        int reviewId, int rating, char anonymousFlag, string? commentText,
        CancellationToken cancellationToken = default)
    {
        if (rating < 1 || rating > 5) return (false, "评分必须在1到5之间");
        if (commentText?.Length > 300) return (false, "评价内容不能超过300字");
        anonymousFlag = anonymousFlag == 'Y' ? 'Y' : 'N';
        commentText = string.IsNullOrWhiteSpace(commentText) ? null : commentText.Trim();
        await using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = (OracleTransaction)(await conn.BeginTransactionAsync(cancellationToken));
        try
        {
            var existing = await reviewsRepository.GetByIdWithLockAsync(reviewId, conn, tx, cancellationToken);
            if (existing == null) { await tx.RollbackAsync(cancellationToken); return (false, "评价不存在"); }
            int oldDelta = existing.CreditDelta;
            int creditDelta = CalculateCreditDelta(rating);
            existing.Rating = rating; existing.AnonymousFlag = anonymousFlag;
            existing.CommentText = commentText; existing.CreditDelta = creditDelta;
            if (!await reviewsRepository.UpdateAsync(existing, conn, tx, cancellationToken))
            { await tx.RollbackAsync(cancellationToken); return (false, "更新评价失败"); }
            int deltaChange = creditDelta - oldDelta;
            if (deltaChange != 0)
            {
                int? rid = await GetRunnerIdByRecordAsync(existing.RecordId, conn, tx, cancellationToken);
                if (rid.HasValue) await UpdateRunnerCreditAsync(rid.Value, deltaChange, conn, tx, cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            return (true, "评价更新成功");
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<(bool Success, string Message)> DeleteReviewAsync(
        int reviewId, CancellationToken cancellationToken = default)
    {
        await using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = (OracleTransaction)(await conn.BeginTransactionAsync(cancellationToken));
        try
        {
            var existing = await reviewsRepository.GetByIdWithLockAsync(reviewId, conn, tx, cancellationToken);
            if (existing == null) { await tx.RollbackAsync(cancellationToken); return (false, "评价不存在"); }
            if (existing.CreditDelta != 0)
            {
                int? rid = await GetRunnerIdByRecordAsync(existing.RecordId, conn, tx, cancellationToken);
                if (rid.HasValue) await UpdateRunnerCreditAsync(rid.Value, -existing.CreditDelta, conn, tx, cancellationToken);
            }
            if (!await reviewsRepository.DeleteAsync(reviewId, conn, tx, cancellationToken))
            {
                await tx.RollbackAsync(cancellationToken);
                return (false, "删除评价失败");
            }
            await tx.CommitAsync(cancellationToken);
            return (true, "评价已删除");
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }

    private static async Task<int?> GetRunnerIdByRecordAsync(
        int recordId, OracleConnection conn, OracleTransaction tx, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.BindByName = true;
        cmd.CommandText = "SELECT runner_id FROM APPUSER.assign_records WHERE record_id = :rid";
        cmd.Parameters.Add(new OracleParameter("rid", recordId));
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj == null || obj == DBNull.Value ? null : Convert.ToInt32(obj);
    }

    private static async Task UpdateRunnerCreditAsync(
        int runnerId, int delta, OracleConnection conn, OracleTransaction tx, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.BindByName = true;
        cmd.CommandText = "UPDATE APPUSER.runners SET credit_score = GREATEST(0, credit_score + :delta) WHERE runner_id = :runnerId";
        cmd.Parameters.Add(new OracleParameter("delta", delta));
        cmd.Parameters.Add(new OracleParameter("runnerId", runnerId));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static int CalculateCreditDelta(int rating) => rating switch
    {
        1 => -10,
        2 => -5,
        3 => 0,
        4 => 3,
        5 => 5,
        _ => 0
    };
}
