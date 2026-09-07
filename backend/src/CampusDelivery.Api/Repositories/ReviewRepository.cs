using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class ReviewRepository(OracleConnectionFactory connectionFactory) : IReviewRepository
{
    private const string ReviewProjection = """
        SELECT rv.review_id,
               rv.task_id,
               rv.record_id,
               t.publisher_user_id,
               rv.rating,
               rv.anonymous_flag,
               rv.comment_text,
               rv.reviewed_at,
               rv.credit_delta
        FROM APPUSER.reviews rv
        JOIN APPUSER.tasks t ON t.task_id = rv.task_id
        """;

    public async Task<Review?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = ReviewProjection + " WHERE rv.review_id = :reviewId";
        command.Parameters.Add(new OracleParameter("reviewId", reviewId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReview(reader) : null;
    }

    public async Task<ReviewWriteContext?> GetWriteContextWithLockAsync(
        int reviewId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT rv.review_id,
                   rv.task_id,
                   rv.record_id,
                   t.publisher_user_id,
                   ar.runner_id,
                   rv.rating,
                   rv.anonymous_flag,
                   rv.comment_text,
                   rv.reviewed_at,
                   rv.credit_delta
            FROM APPUSER.reviews rv
            JOIN APPUSER.tasks t ON t.task_id = rv.task_id
            JOIN APPUSER.assign_records ar
              ON ar.record_id = rv.record_id
             AND ar.task_id = rv.task_id
            WHERE rv.review_id = :reviewId
            FOR UPDATE OF rv.rating
            """;
        command.Parameters.Add(new OracleParameter("reviewId", reviewId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ReviewWriteContext(
            MapReview(reader),
            Convert.ToInt32(reader["runner_id"]));
    }

    public async Task<IReadOnlyList<Review>> GetByTaskIdAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var reviews = new List<Review>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = ReviewProjection + " " + """
             WHERE rv.task_id = :taskId
             ORDER BY rv.reviewed_at DESC, rv.review_id DESC
             """;
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            reviews.Add(MapReview(reader));
        }

        return reviews;
    }

    public async Task<IReadOnlyList<Review>> GetAllPagedAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var reviews = new List<Review>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = ReviewProjection + " " + """
             ORDER BY rv.reviewed_at DESC, rv.review_id DESC
             OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
             """;
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            reviews.Add(MapReview(reader));
        }

        return reviews;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.reviews";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<Review>> GetByPublisherUserIdPagedAsync(
        int publisherUserId,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var reviews = new List<Review>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = ReviewProjection + " " + """
             WHERE t.publisher_user_id = :publisherUserId
             ORDER BY rv.reviewed_at DESC, rv.review_id DESC
             OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
             """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            reviews.Add(MapReview(reader));
        }

        return reviews;
    }

    public async Task<int> GetCountByPublisherUserIdAsync(
        int publisherUserId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*)
            FROM APPUSER.reviews rv
            JOIN APPUSER.tasks t ON t.task_id = rv.task_id
            WHERE t.publisher_user_id = :publisherUserId
            """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> ExistsByTaskIdAsync(
        int taskId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.reviews WHERE task_id = :taskId";
        command.Parameters.Add(new OracleParameter("taskId", taskId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<bool> InsertAsync(
        Review review,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.reviews (
                task_id,
                record_id,
                rating,
                anonymous_flag,
                comment_text,
                reviewed_at,
                credit_delta
            )
            VALUES (
                :taskId,
                :recordId,
                :rating,
                :anonymousFlag,
                :commentText,
                :reviewedAt,
                :creditDelta
            )
            """;
        command.Parameters.Add(new OracleParameter("taskId", review.TaskId));
        command.Parameters.Add(new OracleParameter("recordId", review.RecordId));
        command.Parameters.Add(new OracleParameter("rating", review.Rating));
        command.Parameters.Add(new OracleParameter("anonymousFlag", review.AnonymousFlag));
        command.Parameters.Add(new OracleParameter("commentText", (object?)review.CommentText ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("reviewedAt", review.ReviewedAt));
        command.Parameters.Add(new OracleParameter("creditDelta", review.CreditDelta));
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }
        catch (OracleException exception) when (exception.Number == 1)
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(
        Review review,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.reviews
            SET rating = :rating,
                anonymous_flag = :anonymousFlag,
                comment_text = :commentText,
                credit_delta = :creditDelta
            WHERE review_id = :reviewId
            """;
        command.Parameters.Add(new OracleParameter("rating", review.Rating));
        command.Parameters.Add(new OracleParameter("anonymousFlag", review.AnonymousFlag));
        command.Parameters.Add(new OracleParameter("commentText", (object?)review.CommentText ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("creditDelta", review.CreditDelta));
        command.Parameters.Add(new OracleParameter("reviewId", review.ReviewId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteAsync(
        int reviewId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "DELETE FROM APPUSER.reviews WHERE review_id = :reviewId";
        command.Parameters.Add(new OracleParameter("reviewId", reviewId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> UpdateRunnerCreditAsync(
        int runnerId,
        decimal creditDelta,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.runners
            SET credit_score = LEAST(100, GREATEST(0, credit_score + :creditDelta))
            WHERE runner_id = :runnerId
            """;
        command.Parameters.Add(new OracleParameter("creditDelta", creditDelta));
        command.Parameters.Add(new OracleParameter("runnerId", runnerId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static Review MapReview(OracleDataReader reader)
    {
        return new Review
        {
            ReviewId = Convert.ToInt32(reader["review_id"]),
            TaskId = Convert.ToInt32(reader["task_id"]),
            RecordId = Convert.ToInt32(reader["record_id"]),
            PublisherUserId = Convert.ToInt32(reader["publisher_user_id"]),
            Rating = Convert.ToInt32(reader["rating"]),
            AnonymousFlag = Convert.ToChar(reader["anonymous_flag"]),
            CommentText = reader["comment_text"] == DBNull.Value
                ? null
                : Convert.ToString(reader["comment_text"]),
            ReviewedAt = Convert.ToDateTime(reader["reviewed_at"]),
            CreditDelta = Convert.ToDecimal(reader["credit_delta"])
        };
    }
}
