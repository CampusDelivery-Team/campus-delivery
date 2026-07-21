using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class PaymentRepository
{
    private readonly OracleConnectionFactory _connectionFactory;

    public PaymentRepository(OracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PaymentRecord?> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetByTaskIdInternalAsync(taskId, connection, null, cancellationToken);
    }

    public async Task<PaymentRecord?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT payment_id, task_id, record_id, publisher_user_id, order_amount, pay_amount,
                   pay_method, pay_status, third_trade_no, paid_at, created_at, updated_at
            FROM APPUSER.payments
            WHERE payment_id = :paymentId
            """;
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPayment(reader) : null;
    }

    public async Task<PaymentRecord?> GetByTaskIdWithLockAsync(
        int taskId,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT payment_id, task_id, record_id, publisher_user_id, order_amount, pay_amount,
                   pay_method, pay_status, third_trade_no, paid_at, created_at, updated_at
            FROM APPUSER.payments
            WHERE task_id = :taskId
            FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPayment(reader) : null;
    }

    public async Task<PaymentRecord?> GetByIdWithLockAsync(
        int paymentId,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT payment_id, task_id, record_id, publisher_user_id, order_amount, pay_amount,
                   pay_method, pay_status, third_trade_no, paid_at, created_at, updated_at
            FROM APPUSER.payments
            WHERE payment_id = :paymentId
            FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPayment(reader) : null;
    }

    public async Task<IReadOnlyList<PaymentListRecord>> GetPaymentsAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        List<PaymentListRecord> items = new List<PaymentListRecord>();
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT p.payment_id, p.task_id, p.record_id, t.task_title, p.order_amount, p.pay_amount,
                   p.pay_method, p.pay_status, p.third_trade_no, p.paid_at, p.created_at
            FROM APPUSER.payments p
            JOIN APPUSER.tasks t ON t.task_id = p.task_id
            ORDER BY p.created_at DESC, p.payment_id DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapPaymentListRecord(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<PaymentListRecord>> GetPaymentsByPublisherUserIdAsync(
        int publisherUserId,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        List<PaymentListRecord> items = new List<PaymentListRecord>();
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
     SELECT 
        p.payment_id,
        t.task_id,
        p.record_id,
        t.task_title,
        p.order_amount,
        p.pay_amount,
        p.pay_method,
        p.pay_status,
        p.third_trade_no,
        t.created_at
     FROM APPUSER.payments p
     JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
     JOIN APPUSER.tasks t ON t.task_id = ar.task_id
     WHERE t.publisher_user_id = :publisherUserId
     ORDER BY t.created_at DESC, p.payment_id DESC
     OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
     """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapPaymentListRecord(reader));
        }

        return items;
    }

    public async Task<int> GetPaymentsCountAsync(CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.payments";
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> GetPaymentsCountByPublisherUserIdAsync(
        int publisherUserId,
        CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
        SELECT COUNT(DISTINCT p.payment_id)
        FROM APPUSER.payments p
        JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
        JOIN APPUSER.tasks t ON t.task_id = ar.task_id
        WHERE t.publisher_user_id = :publisherUserId
        """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> InsertAsync(
        PaymentRecord record,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.payments (
                task_id,
                record_id,
                publisher_user_id,
                order_amount,
                pay_amount,
                pay_method,
                third_trade_no,
                pay_status,
                paid_at,
                created_at,
                updated_at
            ) VALUES (
                :taskId,
                :recordId,
                :publisherUserId,
                :orderAmount,
                :payAmount,
                :payMethod,
                :thirdTradeNo,
                :payStatus,
                :paidAt,
                SYSDATE,
                SYSDATE
            )
            RETURNING payment_id INTO :paymentId
            """;
        command.Parameters.Add(new OracleParameter("taskId", record.TaskId));
        command.Parameters.Add(new OracleParameter("recordId", record.RecordId));
        command.Parameters.Add(new OracleParameter("publisherUserId", record.PublisherUserId));
        command.Parameters.Add(new OracleParameter("orderAmount", record.OrderAmount));
        command.Parameters.Add(new OracleParameter("payAmount", record.PayAmount));
        command.Parameters.Add(new OracleParameter("payMethod", record.PayMethod));
        command.Parameters.Add(new OracleParameter("thirdTradeNo", (object?)record.ThirdTradeNo ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("payStatus", record.PayStatus));
        command.Parameters.Add(new OracleParameter("paidAt", record.PaidAt.HasValue ? record.PaidAt.Value : DBNull.Value));

        OracleParameter paymentIdParameter = new OracleParameter("paymentId", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(paymentIdParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return paymentIdParameter.Value is OracleDecimal oracleDecimal
            ? oracleDecimal.ToInt32()
            : Convert.ToInt32(paymentIdParameter.Value);
    }

    public async Task UpdatePaymentStatusAsync(
        int paymentId,
        string payStatus,
        string? thirdTradeNo,
        DateTime? paidAt,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.payments
            SET pay_status = :payStatus,
                third_trade_no = COALESCE(:thirdTradeNo, third_trade_no),
                paid_at = COALESCE(:paidAt, paid_at),
                updated_at = SYSDATE
            WHERE payment_id = :paymentId
            """;
        command.Parameters.Add(new OracleParameter("payStatus", payStatus));
        command.Parameters.Add(new OracleParameter("thirdTradeNo", (object?)thirdTradeNo ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("paidAt", paidAt.HasValue ? paidAt.Value : DBNull.Value));
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdatePaymentAsync(
        int paymentId,
        PaymentRecord record,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.payments
            SET pay_method = :payMethod,
                order_amount = :orderAmount,
                pay_amount = :payAmount,
                third_trade_no = :thirdTradeNo,
                pay_status = :payStatus,
                paid_at = :paidAt,
                updated_at = SYSDATE
            WHERE payment_id = :paymentId
            """;
        command.Parameters.Add(new OracleParameter("payMethod", record.PayMethod));
        command.Parameters.Add(new OracleParameter("orderAmount", record.OrderAmount));
        command.Parameters.Add(new OracleParameter("payAmount", record.PayAmount));
        command.Parameters.Add(new OracleParameter("thirdTradeNo", (object?)record.ThirdTradeNo ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("payStatus", record.PayStatus));
        command.Parameters.Add(new OracleParameter("paidAt", record.PaidAt.HasValue ? record.PaidAt.Value : DBNull.Value));
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PaymentRecord?> GetByTaskIdAsync(
        int taskId,
        OracleConnection connection,
        OracleTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        return await GetByTaskIdInternalAsync(taskId, connection, transaction, cancellationToken);
    }

    private async Task<PaymentRecord?> GetByTaskIdInternalAsync(
      int taskId,
      OracleConnection connection,
      OracleTransaction? transaction,
      CancellationToken cancellationToken)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.Transaction = transaction;
        command.CommandText = """
        SELECT p.payment_id, ar.task_id, p.record_id, t.publisher_user_id, p.order_amount, p.pay_amount,
               p.pay_method, p.pay_status, p.third_trade_no
        FROM APPUSER.payments p
        JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
        JOIN APPUSER.tasks t ON t.task_id = ar.task_id
        WHERE ar.task_id = :taskId
        ORDER BY p.payment_id DESC
        FETCH FIRST 1 ROWS ONLY
        """;
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPayment(reader) : null;
    }

    private static PaymentRecord MapPayment(OracleDataReader reader)
    {
        return new PaymentRecord
        {
            PaymentId = Convert.ToInt32(reader["payment_id"]),
            TaskId = Convert.ToInt32(reader["task_id"]),
            RecordId = Convert.ToInt32(reader["record_id"]),
            PublisherUserId = Convert.ToInt32(reader["publisher_user_id"]),
            OrderAmount = Convert.ToDecimal(reader["order_amount"]),
            PayAmount = Convert.ToDecimal(reader["pay_amount"]),
            PayMethod = Convert.ToString(reader["pay_method"]) ?? "CASH",
            PayStatus = Convert.ToString(reader["pay_status"]) ?? "UNPAID",
            ThirdTradeNo = reader["third_trade_no"] == DBNull.Value ? null : Convert.ToString(reader["third_trade_no"]),
            PaidAt = reader["paid_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["paid_at"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"])
        };
    }

    private static PaymentListRecord MapPaymentListRecord(OracleDataReader reader)
    {
        return new PaymentListRecord
        {
            PaymentId = Convert.ToInt32(reader["payment_id"]),
            TaskId = Convert.ToInt32(reader["task_id"]),
            RecordId = Convert.ToInt32(reader["record_id"]),
            TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
            OrderAmount = Convert.ToDecimal(reader["order_amount"]),
            PayAmount = Convert.ToDecimal(reader["pay_amount"]),
            PayMethod = Convert.ToString(reader["pay_method"]) ?? "CASH",
            PayStatus = Convert.ToString(reader["pay_status"]) ?? "UNPAID",
            ThirdTradeNo = reader["third_trade_no"] == DBNull.Value ? null : Convert.ToString(reader["third_trade_no"]),
            PaidAt = reader["paid_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["paid_at"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"])
        };
    }
}
