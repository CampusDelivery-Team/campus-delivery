using System.Data;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CampusDelivery.Api.Repositories;

/// <summary>退款表访问，仅使用 refunds 的实际字段。</summary>
public sealed class RefundRepository(OracleConnectionFactory connectionFactory)
{
    private const string RefundProjection = """
        SELECT refund_id, payment_id, refund_amount, refund_reason, approved_amount, process_status
          FROM APPUSER.refunds
        """;

    public async Task<RefundRecord?> GetByIdAsync(int refundId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetByIdAsync(refundId, connection, null, false, cancellationToken);
    }

    public async Task<RefundRecord?> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = RefundProjection + " WHERE payment_id = :paymentId ORDER BY refund_id DESC FETCH FIRST 1 ROWS ONLY";
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRefund(reader) : null;
    }

    public async Task<RefundRecord?> GetActiveByPaymentIdWithLockAsync(
        int paymentId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = RefundProjection + " " + """
            WHERE payment_id = :paymentId
              AND process_status IN ('APPLY', 'APPROVED')
            FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRefund(reader) : null;
    }

    public Task<RefundRecord?> GetByIdWithLockAsync(
        int refundId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default) =>
        GetByIdAsync(refundId, connection, transaction, true, cancellationToken);

    public async Task<IReadOnlyList<RefundListRecord>> GetRefundsAsync(int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        List<RefundListRecord> items = [];
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT r.refund_id, r.payment_id, ar.task_id, t.task_title,
                   r.refund_amount, r.refund_reason, r.approved_amount, r.process_status
              FROM APPUSER.refunds r
              JOIN APPUSER.payments p ON p.payment_id = r.payment_id
              JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
              JOIN APPUSER.tasks t ON t.task_id = ar.task_id
             ORDER BY r.refund_id DESC
             OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new RefundListRecord
            {
                RefundId = Convert.ToInt32(reader["refund_id"]),
                PaymentId = Convert.ToInt32(reader["payment_id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
                RefundAmount = Convert.ToDecimal(reader["refund_amount"]),
                RefundReason = Convert.ToString(reader["refund_reason"]),
                ApprovedAmount = reader["approved_amount"] == DBNull.Value ? null : Convert.ToDecimal(reader["approved_amount"]),
                ProcessStatus = Convert.ToString(reader["process_status"]) ?? "APPLY"
            });
        }

        return items;
    }

    public async Task<int> GetRefundsCountAsync(CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.refunds";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<int> InsertAsync(RefundRecord record, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.refunds (payment_id, refund_amount, refund_reason, approved_amount, process_status)
            VALUES (:paymentId, :refundAmount, :refundReason, :approvedAmount, :processStatus)
            RETURNING refund_id INTO :refundId
            """;
        command.Parameters.Add(new OracleParameter("paymentId", record.PaymentId));
        command.Parameters.Add(new OracleParameter("refundAmount", record.RefundAmount));
        command.Parameters.Add(new OracleParameter("refundReason", record.RefundReason));
        command.Parameters.Add(new OracleParameter("approvedAmount", (object?)record.ApprovedAmount ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("processStatus", record.ProcessStatus));
        OracleParameter refundId = new("refundId", OracleDbType.Int32) { Direction = ParameterDirection.Output };
        command.Parameters.Add(refundId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return refundId.Value is OracleDecimal value ? value.ToInt32() : Convert.ToInt32(refundId.Value);
    }

    public async Task UpdateReviewAsync(int refundId, string processStatus, decimal approvedAmount, string combinedReason, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.refunds
               SET process_status = :processStatus,
                   approved_amount = :approvedAmount,
                   refund_reason = :combinedReason
             WHERE refund_id = :refundId
            """;
        command.Parameters.Add(new OracleParameter("processStatus", processStatus));
        command.Parameters.Add(new OracleParameter("approvedAmount", approvedAmount));
        command.Parameters.Add(new OracleParameter("combinedReason", combinedReason));
        command.Parameters.Add(new OracleParameter("refundId", refundId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<RefundRecord?> GetByIdAsync(int refundId, OracleConnection connection, OracleTransaction? transaction, bool forUpdate, CancellationToken cancellationToken)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = RefundProjection + " WHERE refund_id = :refundId" + (forUpdate ? " FOR UPDATE" : string.Empty);
        command.Parameters.Add(new OracleParameter("refundId", refundId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRefund(reader) : null;
    }

    private static RefundRecord MapRefund(OracleDataReader reader) => new()
    {
        RefundId = Convert.ToInt32(reader["refund_id"]),
        PaymentId = Convert.ToInt32(reader["payment_id"]),
        RefundAmount = Convert.ToDecimal(reader["refund_amount"]),
        RefundReason = Convert.ToString(reader["refund_reason"]) ?? string.Empty,
        ApprovedAmount = reader["approved_amount"] == DBNull.Value ? null : Convert.ToDecimal(reader["approved_amount"]),
        ProcessStatus = Convert.ToString(reader["process_status"]) ?? "APPLY"
    };
}
