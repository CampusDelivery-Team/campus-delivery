using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class ReviewsRepository(OracleConnectionFactory connectionFactory)
{
    public async Task<Review?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews WHERE review_id = :id
            """;
        command.Parameters.Add(new OracleParameter("id", reviewId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReview(reader) : null;
    }

    public async Task<Review?> GetByRecordIdAsync(int recordId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews WHERE record_id = :recordId
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReview(reader) : null;
    }

    public async Task<Review?> GetByIdWithLockAsync(
        int reviewId, OracleConnection connection, OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews WHERE review_id = :id FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("id", reviewId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReview(reader) : null;
    }

    public async Task<int?> GetEligibleRunnerIdAsync(
        int recordId, int publisherUserId, OracleConnection connection, OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT ar.runner_id
            FROM APPUSER.assign_records ar
            JOIN APPUSER.tasks t ON ar.task_id = t.task_id
            WHERE ar.record_id = :recordId
              AND t.publisher_user_id = :publisherUserId
              AND t.task_status = 'FINISHED'
            FOR UPDATE OF ar.runner_id
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
    }

    public async Task<Review?> GetByRecordIdAsync(
        int recordId, OracleConnection connection, OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews WHERE record_id = :recordId
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReview(reader) : null;
    }

    public async Task<bool> CanCreateAsync(
        int recordId, int publisherUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*)
            FROM APPUSER.assign_records ar
            JOIN APPUSER.tasks t ON ar.task_id = t.task_id
            WHERE ar.record_id = :recordId
              AND t.publisher_user_id = :publisherUserId
              AND t.task_status = 'FINISHED'
              AND NOT EXISTS (
                  SELECT 1 FROM APPUSER.reviews rv WHERE rv.record_id = ar.record_id
              )
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<IReadOnlyList<Review>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var list = new List<Review>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT rv.review_id, rv.record_id, rv.rating, rv.anonymous_flag, rv.comment_text, rv.reviewed_at, rv.credit_delta
            FROM APPUSER.reviews rv
            JOIN APPUSER.assign_records ar ON rv.record_id = ar.record_id
            WHERE ar.task_id = :taskId
            ORDER BY rv.reviewed_at DESC
            """;
        command.Parameters.Add(new OracleParameter("taskId", taskId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapReview(reader));
        }
        return list;
    }

    public async Task<IReadOnlyList<Review>> GetAllPagedAsync(int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var list = new List<Review>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews
            ORDER BY reviewed_at DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapReview(reader));
        }
        return list;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.reviews";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<IReadOnlyList<Review>> GetByPublisherUserIdAsync(
        int publisherUserId, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var list = new List<Review>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT rv.review_id, rv.record_id, rv.rating, rv.anonymous_flag,
                   rv.comment_text, rv.reviewed_at, rv.credit_delta
            FROM APPUSER.reviews rv
            JOIN APPUSER.assign_records ar ON rv.record_id = ar.record_id
            JOIN APPUSER.tasks t ON ar.task_id = t.task_id
            WHERE t.publisher_user_id = :publisherUserId
            ORDER BY rv.reviewed_at DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapReview(reader));
        }
        return list;
    }

    public async Task<int> GetCountByPublisherUserIdAsync(
        int publisherUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*)
            FROM APPUSER.reviews rv
            JOIN APPUSER.assign_records ar ON rv.record_id = ar.record_id
            JOIN APPUSER.tasks t ON ar.task_id = t.task_id
            WHERE t.publisher_user_id = :publisherUserId
            """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> InsertAsync(Review review, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.reviews (record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta)
            VALUES (:recordId, :rating, :anonymousFlag, :commentText, :reviewedAt, :creditDelta)
            """;
        command.Parameters.Add(new OracleParameter("recordId", review.RecordId));
        command.Parameters.Add(new OracleParameter("rating", review.Rating));
        command.Parameters.Add(new OracleParameter("anonymousFlag", review.AnonymousFlag));
        command.Parameters.Add(new OracleParameter("commentText", (object?)review.CommentText ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("reviewedAt", review.ReviewedAt));
        command.Parameters.Add(new OracleParameter("creditDelta", review.CreditDelta));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateAsync(Review review, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.reviews
            SET rating = :rating, anonymous_flag = :anonymousFlag, comment_text = :commentText, credit_delta = :creditDelta
            WHERE review_id = :reviewId
            """;
        command.Parameters.Add(new OracleParameter("rating", review.Rating));
        command.Parameters.Add(new OracleParameter("anonymousFlag", review.AnonymousFlag));
        command.Parameters.Add(new OracleParameter("commentText", (object?)review.CommentText ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("creditDelta", review.CreditDelta));
        command.Parameters.Add(new OracleParameter("reviewId", review.ReviewId));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int reviewId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "DELETE FROM APPUSER.reviews WHERE review_id = :reviewId";
        command.Parameters.Add(new OracleParameter("reviewId", reviewId));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Review MapReview(OracleDataReader reader)
    {
        return new Review
        {
            ReviewId = Convert.ToInt32(reader["review_id"]),
            RecordId = Convert.ToInt32(reader["record_id"]),
            Rating = Convert.ToInt32(reader["rating"]),
            AnonymousFlag = Convert.ToChar(reader["anonymous_flag"]),
            CommentText = reader["comment_text"] == DBNull.Value ? null : Convert.ToString(reader["comment_text"]),
            ReviewedAt = Convert.ToDateTime(reader["reviewed_at"]),
            CreditDelta = Convert.ToInt32(reader["credit_delta"])
        };
    }
}
