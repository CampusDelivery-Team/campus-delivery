using CampusDelivery.Api.Models;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public class ReviewsRepository
{
    private readonly string _connectionString;

    public ReviewsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleDb")
            ?? throw new InvalidOperationException("Connection string 'OracleDb' is missing.");
    }

    // 根据评价ID获取单条评价

    public Review? GetReviewById(int reviewId)
    {
        using var conn = new OracleConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT review_id, report_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews
            WHERE review_id = :reviewId";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("reviewId", reviewId));

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReview(reader) : null;
    }

    
    // 根据报告ID获取所有评价（可用于查看某次报告的所有评价）
    
    public List<Review> GetReviewsByReportId(int reportId)
    {
        var list = new List<Review>();
        using var conn = new OracleConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT review_id, report_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews
            WHERE report_id = :reportId
            ORDER BY reviewed_at DESC";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("reportId", reportId));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReview(reader));
        }
        return list;
    }

   
    // 获取所有评价

    public List<Review> GetAllReviews(int pageNumber = 1, int pageSize = 20)
    {
        var list = new List<Review>();
        var offset = (pageNumber - 1) * pageSize;

        using var conn = new OracleConnection(_connectionString);
        conn.Open();

        string sql = @"
            SELECT review_id, report_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta
            FROM APPUSER.reviews
            ORDER BY reviewed_at DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("offset", offset));
        cmd.Parameters.Add(new OracleParameter("pageSize", pageSize));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapReview(reader));
        }
        return list;
    }

    // 插入一条新评价

    public bool InsertReview(Review review)
    {
        using var conn = new OracleConnection(_connectionString);
        conn.Open();

        const string sql = @"
            INSERT INTO APPUSER.reviews 
                (report_id, rating, anonymous_flag, comment_text, reviewed_at, credit_delta)
            VALUES 
                (:reportId, :rating, :anonymousFlag, :commentText, :reviewedAt, :creditDelta)";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("reportId", review.ReportID));
        cmd.Parameters.Add(new OracleParameter("rating", review.Rating));
        cmd.Parameters.Add(new OracleParameter("anonymousFlag", review.Anonymous_flag));
        cmd.Parameters.Add(new OracleParameter("commentText", review.Comment_text ?? (object)DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("reviewedAt", review.Reviewed_at));
        cmd.Parameters.Add(new OracleParameter("creditDelta", review.Credit_delta));

        return cmd.ExecuteNonQuery() > 0;
    }


    // 更新评价内容（例如修改评价文字或评分，但不修改创建时间）

    public bool UpdateReview(Review review)
    {
        using var conn = new OracleConnection(_connectionString);
        conn.Open();

        const string sql = @"
            UPDATE APPUSER.reviews
            SET rating = :rating,
                anonymous_flag = :anonymousFlag,
                comment_text = :commentText,
                credit_delta = :creditDelta
            WHERE review_id = :reviewId";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("rating", review.Rating));
        cmd.Parameters.Add(new OracleParameter("anonymousFlag", review.Anonymous_flag));
        cmd.Parameters.Add(new OracleParameter("commentText", review.Comment_text ?? (object)DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("creditDelta", review.Credit_delta));
        cmd.Parameters.Add(new OracleParameter("reviewId", review.ReviewID));

        return cmd.ExecuteNonQuery() > 0;
    }

    // 删除评价

    public bool DeleteReview(int reviewId)
    {
        using var conn = new OracleConnection(_connectionString);
        conn.Open();

        const string sql = "DELETE FROM APPUSER.reviews WHERE review_id = :reviewId";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("reviewId", reviewId));

        return cmd.ExecuteNonQuery() > 0;
    }

    // 获取评价总数
    public int GetTotalCount()
    {
        using var conn = new OracleConnection(_connectionString);
        conn.Open();

        const string sql = "SELECT COUNT(*) FROM APPUSER.reviews";

        using var cmd = new OracleCommand(sql, conn);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // 私有映射方法 
    private static Review MapReview(OracleDataReader reader)
    {
        return new Review
        {
            ReviewID = Convert.ToInt32(reader["review_id"]),
            ReportID = reader["report_id"] == DBNull.Value ? null : Convert.ToInt32(reader["report_id"]),
            Rating = reader["rating"] == DBNull.Value ? null : Convert.ToInt32(reader["rating"]),
            Anonymous_flag = Convert.ToChar(reader["anonymous_flag"]),
            Comment_text = reader["comment_text"]?.ToString(),
            Reviewed_at = Convert.ToDateTime(reader["reviewed_at"]),
            Credit_delta = Convert.ToInt32(reader["credit_delta"])
        };
    }
}
