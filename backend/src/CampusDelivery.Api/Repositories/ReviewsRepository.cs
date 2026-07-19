using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class ReviewsRepository(OracleConnectionFactory connectionFactory)
{
    public Review? GetReviewById(int reviewId)
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews
            WHERE review_id = :reviewId
            """;
        command.Parameters.Add(new OracleParameter("reviewId", reviewId));
        using OracleDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapReview(reader) : null;
    }

    public Review? GetReviewByRecordId(int recordId)
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews
            WHERE record_id = :recordId
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        using OracleDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapReview(reader) : null;
    }

    public IReadOnlyList<Review> GetAllReviews(int pageNumber, int pageSize)
    {
        List<Review> reviews = new();
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT review_id, record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews
            ORDER BY reviewed_at DESC, review_id DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("offset", (pageNumber - 1) * pageSize));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));
        using OracleDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            reviews.Add(MapReview(reader));
        }
        return reviews;
    }

    public int GetTotalCount()
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.reviews";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public bool CanReview(int recordId, int publisherUserId)
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*)
            FROM APPUSER.assign_records ar
            JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            WHERE ar.record_id = :recordId
              AND t.publisher_user_id = :publisherUserId
              AND t.task_status = 'FINISHED'
              AND NOT EXISTS (
                  SELECT 1 FROM APPUSER.reviews r WHERE r.record_id = ar.record_id
              )
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool InsertReview(Review review, int publisherUserId)
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleTransaction transaction = connection.BeginTransaction();

        try
        {
            using OracleCommand verify = connection.CreateCommand();
            verify.Transaction = transaction;
            verify.BindByName = true;
            verify.CommandText = """
                SELECT ar.runner_id
                FROM APPUSER.assign_records ar
                JOIN APPUSER.tasks t ON t.task_id = ar.task_id
                WHERE ar.record_id = :recordId
                  AND t.publisher_user_id = :publisherUserId
                  AND t.task_status = 'FINISHED'
                  AND NOT EXISTS (SELECT 1 FROM APPUSER.reviews r WHERE r.record_id = ar.record_id)
                FOR UPDATE
                """;
            verify.Parameters.Add(new OracleParameter("recordId", review.RecordId));
            verify.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
            object? runnerId = verify.ExecuteScalar();
            if (runnerId == null || runnerId == DBNull.Value)
            {
                transaction.Rollback();
                return false;
            }

            review.CreditDelta = GetCreditDelta(review.Rating);
            using OracleCommand insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.BindByName = true;
            insert.CommandText = """
                INSERT INTO APPUSER.reviews
                    (record_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta)
                VALUES
                    (:recordId, :rating, :anonymousFlag, :commentText, SYSDATE, :creditDelta)
                """;
            insert.Parameters.Add(new OracleParameter("recordId", review.RecordId));
            insert.Parameters.Add(new OracleParameter("rating", review.Rating));
            insert.Parameters.Add(new OracleParameter("anonymousFlag", review.AnonymousFlag));
            insert.Parameters.Add(new OracleParameter("commentText", (object?)review.CommentText ?? DBNull.Value));
            insert.Parameters.Add(new OracleParameter("creditDelta", review.CreditDelta));
            insert.ExecuteNonQuery();

            UpdateRunnerCredit(connection, transaction, Convert.ToInt32(runnerId), review.CreditDelta);
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool UpdateReview(Review review)
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleTransaction transaction = connection.BeginTransaction();

        try
        {
            using OracleCommand current = connection.CreateCommand();
            current.Transaction = transaction;
            current.BindByName = true;
            current.CommandText = """
                SELECT r.credit_delta, ar.runner_id
                FROM APPUSER.reviews r
                JOIN APPUSER.assign_records ar ON ar.record_id = r.record_id
                WHERE r.review_id = :reviewId
                FOR UPDATE
                """;
            current.Parameters.Add(new OracleParameter("reviewId", review.ReviewId));
            using OracleDataReader reader = current.ExecuteReader();
            if (!reader.Read())
            {
                transaction.Rollback();
                return false;
            }
            int oldDelta = Convert.ToInt32(reader["credit_delta"]);
            int runnerId = Convert.ToInt32(reader["runner_id"]);
            reader.Close();

            review.CreditDelta = GetCreditDelta(review.Rating);
            using OracleCommand update = connection.CreateCommand();
            update.Transaction = transaction;
            update.BindByName = true;
            update.CommandText = """
                UPDATE APPUSER.reviews
                SET rating = :rating,
                    anonymous_flag = :anonymousFlag,
                    comment_text = :commentText,
                    credit_delta = :creditDelta
                WHERE review_id = :reviewId
                """;
            update.Parameters.Add(new OracleParameter("rating", review.Rating));
            update.Parameters.Add(new OracleParameter("anonymousFlag", review.AnonymousFlag));
            update.Parameters.Add(new OracleParameter("commentText", (object?)review.CommentText ?? DBNull.Value));
            update.Parameters.Add(new OracleParameter("creditDelta", review.CreditDelta));
            update.Parameters.Add(new OracleParameter("reviewId", review.ReviewId));
            update.ExecuteNonQuery();

            UpdateRunnerCredit(connection, transaction, runnerId, review.CreditDelta - oldDelta);
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool DeleteReview(int reviewId)
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleTransaction transaction = connection.BeginTransaction();

        try
        {
            using OracleCommand current = connection.CreateCommand();
            current.Transaction = transaction;
            current.BindByName = true;
            current.CommandText = """
                SELECT r.credit_delta, ar.runner_id
                FROM APPUSER.reviews r
                JOIN APPUSER.assign_records ar ON ar.record_id = r.record_id
                WHERE r.review_id = :reviewId
                FOR UPDATE
                """;
            current.Parameters.Add(new OracleParameter("reviewId", reviewId));
            using OracleDataReader reader = current.ExecuteReader();
            if (!reader.Read())
            {
                transaction.Rollback();
                return false;
            }
            int creditDelta = Convert.ToInt32(reader["credit_delta"]);
            int runnerId = Convert.ToInt32(reader["runner_id"]);
            reader.Close();

            using OracleCommand delete = connection.CreateCommand();
            delete.Transaction = transaction;
            delete.BindByName = true;
            delete.CommandText = "DELETE FROM APPUSER.reviews WHERE review_id = :reviewId";
            delete.Parameters.Add(new OracleParameter("reviewId", reviewId));
            delete.ExecuteNonQuery();

            UpdateRunnerCredit(connection, transaction, runnerId, -creditDelta);
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void UpdateRunnerCredit(
        OracleConnection connection,
        OracleTransaction transaction,
        int runnerId,
        int delta)
    {
        using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.runners
            SET credit_score = GREATEST(0, credit_score + :delta)
            WHERE runner_id = :runnerId
            """;
        command.Parameters.Add(new OracleParameter("delta", delta));
        command.Parameters.Add(new OracleParameter("runnerId", runnerId));
        command.ExecuteNonQuery();
    }

    private static int GetCreditDelta(int rating) => rating switch
    {
        1 => -10,
        2 => -5,
        3 => 0,
        4 => 3,
        5 => 5,
        _ => 0
    };

    private static Review MapReview(OracleDataReader reader) => new()
    {
        ReviewId = Convert.ToInt32(reader["review_id"]),
        RecordId = Convert.ToInt32(reader["record_id"]),
        Rating = Convert.ToInt32(reader["rating"]),
        AnonymousFlag = Convert.ToString(reader["anonymous_flag"]) ?? "N",
        CommentText = reader["comment_text"] == DBNull.Value ? null : Convert.ToString(reader["comment_text"]),
        ReviewedAt = Convert.ToDateTime(reader["reviewed_at"]),
        CreditDelta = Convert.ToInt32(reader["credit_delta"])
    };
}
