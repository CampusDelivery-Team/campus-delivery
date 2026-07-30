using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class RefundRepository
{
    private readonly OracleConnectionFactory _connectionFactory;

    public RefundRepository(OracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RefundRecord?> GetByIdAsync(int refundId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetByIdInternalAsync(refundId, connection, null, cancellationToken);
    }

    public async Task<RefundRecord?> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetByPaymentIdInternalAsync(paymentId, connection, null, cancellationToken);
    }

    public async Task<RefundRecord?> GetByPaymentIdWithLockAsync(
        int paymentId,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT refund_id, payment_id, task_id, record_id, request_user_id, refund_amount,
                   refund_status, refund_reason, review_reason, reviewed_by_user_id,
                   reviewed_at, created_at, updated_at
            FROM APPUSER.refunds
            WHERE payment_id = :paymentId
            FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRefund(reader) : null;
    }

    public async Task<RefundRecord?> GetByIdWithLockAsync(
        int refundId,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT refund_id, payment_id, task_id, record_id, request_user_id, refund_amount,
                   refund_status, refund_reason, review_reason, reviewed_by_user_id,
                   reviewed_at, created_at, updated_at
            FROM APPUSER.refunds
            WHERE refund_id = :refundId
            FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("refundId", refundId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRefund(reader) : null;
    }

    public async Task<IReadOnlyList<RefundListRecord>> GetRefundsAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        List<RefundListRecord> items = new List<RefundListRecord>();
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
        SELECT DISTINCT 
            r.refund_id, 
            r.payment_id, 
            t.task_id, 
            t.task_title, 
            r.refund_amount,
            r.process_status AS refund_status, 
            r.refund_reason, 
            r.approved_amount,
            t.created_at AS created_at
        FROM APPUSER.refunds r
        JOIN APPUSER.payments p ON p.payment_id = r.payment_id
        JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
        JOIN APPUSER.tasks t ON t.task_id = ar.task_id
        ORDER BY t.created_at DESC, r.refund_id DESC
        OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
        """;
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapRefundListRecord(reader));
        }

        return items;
    }
    public async Task<int> GetRefundsCountAsync(CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.refunds";
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> InsertAsync(
        RefundRecord record,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.refunds (
                payment_id,
                task_id,
                record_id,
                request_user_id,
                refund_amount,
                refund_status,
                refund_reason,
                review_reason,
                reviewed_by_user_id,
                reviewed_at,
                created_at,
                updated_at
            ) VALUES (
                :paymentId,
                :taskId,
                :recordId,
                :requestUserId,
                :refundAmount,
                :refundStatus,
                :refundReason,
                :reviewReason,
                :reviewedByUserId,
                :reviewedAt,
                SYSDATE,
                SYSDATE
            )
            RETURNING refund_id INTO :refundId
            """;
        command.Parameters.Add(new OracleParameter("paymentId", record.PaymentId));
        command.Parameters.Add(new OracleParameter("taskId", record.TaskId));
        command.Parameters.Add(new OracleParameter("recordId", record.RecordId));
        command.Parameters.Add(new OracleParameter("requestUserId", record.RequestUserId));
        command.Parameters.Add(new OracleParameter("refundAmount", record.RefundAmount));
        command.Parameters.Add(new OracleParameter("refundStatus", record.RefundStatus));
        command.Parameters.Add(new OracleParameter("refundReason", (object?)record.RefundReason ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("reviewReason", (object?)record.ReviewReason ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("reviewedByUserId", (object?)record.ReviewedByUserId ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("reviewedAt", (object?)record.ReviewedAt ?? DBNull.Value));

        OracleParameter refundIdParameter = new OracleParameter("refundId", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(refundIdParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return refundIdParameter.Value is OracleDecimal oracleDecimal
            ? oracleDecimal.ToInt32()
            : Convert.ToInt32(refundIdParameter.Value);
    }

    public async Task UpdateRefundStatusAsync(
        int refundId,
        string refundStatus,
        string? reviewReason,
        int? reviewedByUserId,
        DateTime? reviewedAt,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.refunds
            SET refund_status = :refundStatus,
                review_reason = COALESCE(:reviewReason, review_reason),
                reviewed_by_user_id = COALESCE(:reviewedByUserId, reviewed_by_user_id),
                reviewed_at = COALESCE(:reviewedAt, reviewed_at),
                updated_at = SYSDATE
            WHERE refund_id = :refundId
            """;
        command.Parameters.Add(new OracleParameter("refundStatus", refundStatus));
        command.Parameters.Add(new OracleParameter("reviewReason", (object?)reviewReason ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("reviewedByUserId", (object?)reviewedByUserId ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("reviewedAt", (object?)reviewedAt ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("refundId", refundId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<RefundRecord?> GetByIdInternalAsync(
        int refundId,
        OracleConnection connection,
        OracleTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT refund_id, payment_id, task_id, record_id, request_user_id, refund_amount,
                   refund_status, refund_reason, review_reason, reviewed_by_user_id,
                   reviewed_at, created_at, updated_at
            FROM APPUSER.refunds
            WHERE refund_id = :refundId
            """;
        command.Parameters.Add(new OracleParameter("refundId", refundId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRefund(reader) : null;
    }

    private async Task<RefundRecord?> GetByPaymentIdInternalAsync(
        int paymentId,
        OracleConnection connection,
        OracleTransaction? transaction,
        CancellationToken cancellationToken,
        bool forUpdate = false)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = $"""
            SELECT refund_id, payment_id, task_id, record_id, request_user_id, refund_amount,
                   refund_status, refund_reason, review_reason, reviewed_by_user_id,
                   reviewed_at, created_at, updated_at
            FROM APPUSER.refunds
            WHERE payment_id = :paymentId
            ORDER BY created_at DESC, refund_id DESC
            FETCH FIRST 1 ROWS ONLY{(forUpdate ? " FOR UPDATE" : string.Empty)}
            """;
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRefund(reader) : null;
    }

    private static RefundRecord MapRefund(OracleDataReader reader)
    {
        return new RefundRecord
        {
            RefundId = Convert.ToInt32(reader["refund_id"]),
            PaymentId = Convert.ToInt32(reader["payment_id"]),
            TaskId = Convert.ToInt32(reader["task_id"]),
            RecordId = Convert.ToInt32(reader["record_id"]),
            RequestUserId = Convert.ToInt32(reader["request_user_id"]),
            RefundAmount = Convert.ToDecimal(reader["refund_amount"]),
            RefundStatus = Convert.ToString(reader["refund_status"]) ?? "PENDING",
            RefundReason = reader["refund_reason"] == DBNull.Value ? null : Convert.ToString(reader["refund_reason"]),
            ReviewReason = reader["review_reason"] == DBNull.Value ? null : Convert.ToString(reader["review_reason"]),
            ReviewedByUserId = reader["reviewed_by_user_id"] == DBNull.Value ? null : Convert.ToInt32(reader["reviewed_by_user_id"]),
            ReviewedAt = reader["reviewed_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["reviewed_at"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"])
        };
    }

    private static RefundListRecord MapRefundListRecord(OracleDataReader reader)
    {
        return new RefundListRecord
        {
            RefundId = Convert.ToInt32(reader["refund_id"]),
            PaymentId = Convert.ToInt32(reader["payment_id"]),
            TaskId = Convert.ToInt32(reader["task_id"]),
            TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
            RefundAmount = Convert.ToDecimal(reader["refund_amount"]),
            RefundStatus = Convert.ToString(reader["refund_status"]) ?? "PENDING",
            RefundReason = reader["refund_reason"] == DBNull.Value ? null : Convert.ToString(reader["refund_reason"]),
            ReviewReason = reader["review_reason"] == DBNull.Value ? null : Convert.ToString(reader["review_reason"]),
            ReviewedByName = reader["reviewed_by_name"] == DBNull.Value ? null : Convert.ToString(reader["reviewed_by_name"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            ReviewedAt = reader["reviewed_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["reviewed_at"])
        };
    }
}
