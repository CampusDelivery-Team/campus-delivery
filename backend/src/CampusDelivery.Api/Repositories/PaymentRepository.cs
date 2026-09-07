using System.Data;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CampusDelivery.Api.Repositories;

/// <summary>支付表访问。payments 与任务的关系只能通过 record_id -> assign_records 建立。</summary>
public sealed class PaymentRepository(OracleConnectionFactory connectionFactory) : IPaymentRepository
{
    private const string PaymentProjection = """
        SELECT p.payment_id, ar.task_id, p.record_id, t.publisher_user_id, t.task_title,
               p.order_amount, p.pay_amount, p.pay_method, p.third_trade_no, p.pay_status
          FROM APPUSER.payments p
          JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
          JOIN APPUSER.tasks t ON t.task_id = ar.task_id
        """;

    public async Task<PaymentRecord?> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetByTaskIdAsync(taskId, connection, null, cancellationToken);
    }

    public async Task<PaymentRecord?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetByIdAsync(paymentId, connection, null, false, cancellationToken);
    }

    public Task<PaymentRecord?> GetByTaskIdWithLockAsync(
        int taskId, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        return GetByTaskIdAsync(taskId, connection, transaction, cancellationToken, true);
    }

    public Task<PaymentRecord?> GetByIdWithLockAsync(
        int paymentId, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        return GetByIdAsync(paymentId, connection, transaction, true, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentListRecord>> GetPaymentsByPublisherUserIdAsync(
        int publisherUserId, string? keyword, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        List<PaymentListRecord> items = [];
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT payment_id,
                   task_id,
                   record_id,
                   task_title,
                   order_amount,
                   pay_amount,
                   pay_method,
                   third_trade_no,
                   pay_status,
                   latest_refund_status AS refund_process_status,
                   is_settled
              FROM APPUSER.vw_payment_refund_overview
             WHERE publisher_user_id = :publisherUserId
               AND (:keyword IS NULL OR task_title LIKE :keyword)
             ORDER BY payment_id DESC
             OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        command.Parameters.Add(new OracleParameter("keyword", string.IsNullOrWhiteSpace(keyword) ? DBNull.Value : $"%{keyword.Trim()}%"));
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));

        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapPaymentList(reader));
        }

        return items;
    }

    public async Task<int> GetPaymentsCountByPublisherUserIdAsync(int publisherUserId, string? keyword, CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*)
              FROM APPUSER.vw_payment_refund_overview
             WHERE publisher_user_id = :publisherUserId
               AND (:keyword IS NULL OR task_title LIKE :keyword)
            """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));
        command.Parameters.Add(new OracleParameter("keyword", string.IsNullOrWhiteSpace(keyword) ? DBNull.Value : $"%{keyword.Trim()}%"));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<int> InsertAsync(PaymentRecord record, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.payments (
                record_id, order_amount, pay_amount, pay_method, third_trade_no, pay_status
            ) VALUES (
                :recordId, :orderAmount, :payAmount, :payMethod, :thirdTradeNo, :payStatus
            ) RETURNING payment_id INTO :paymentId
            """;
        command.Parameters.Add(new OracleParameter("recordId", record.RecordId));
        command.Parameters.Add(new OracleParameter("orderAmount", record.OrderAmount));
        command.Parameters.Add(new OracleParameter("payAmount", record.PayAmount));
        command.Parameters.Add(new OracleParameter("payMethod", record.PayMethod));
        command.Parameters.Add(new OracleParameter("thirdTradeNo", (object?)record.ThirdTradeNo ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("payStatus", record.PayStatus));
        OracleParameter paymentId = new("paymentId", OracleDbType.Int32) { Direction = ParameterDirection.Output };
        command.Parameters.Add(paymentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return paymentId.Value is OracleDecimal value ? value.ToInt32() : Convert.ToInt32(paymentId.Value);
    }

    public async Task UpdatePaymentAsync(int paymentId, PaymentRecord record, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.payments
               SET order_amount = :orderAmount,
                   pay_amount = :payAmount,
                   pay_method = :payMethod,
                   third_trade_no = :thirdTradeNo,
                   pay_status = :payStatus
             WHERE payment_id = :paymentId
            """;
        command.Parameters.Add(new OracleParameter("orderAmount", record.OrderAmount));
        command.Parameters.Add(new OracleParameter("payAmount", record.PayAmount));
        command.Parameters.Add(new OracleParameter("payMethod", record.PayMethod));
        command.Parameters.Add(new OracleParameter("thirdTradeNo", (object?)record.ThirdTradeNo ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("payStatus", record.PayStatus));
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdatePaymentStatusAsync(int paymentId, string payStatus, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "UPDATE APPUSER.payments SET pay_status = :payStatus WHERE payment_id = :paymentId";
        command.Parameters.Add(new OracleParameter("payStatus", payStatus));
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<PaymentRecord?> GetByTaskIdAsync(int taskId, OracleConnection connection, OracleTransaction? transaction, CancellationToken cancellationToken, bool forUpdate = false)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = PaymentProjection + " WHERE ar.task_id = :taskId" + (forUpdate ? " FOR UPDATE OF p.pay_status" : string.Empty);
        command.Parameters.Add(new OracleParameter("taskId", taskId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPayment(reader) : null;
    }

    private static async Task<PaymentRecord?> GetByIdAsync(int paymentId, OracleConnection connection, OracleTransaction? transaction, bool forUpdate, CancellationToken cancellationToken)
    {
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = PaymentProjection + " WHERE p.payment_id = :paymentId" + (forUpdate ? " FOR UPDATE OF p.pay_status" : string.Empty);
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPayment(reader) : null;
    }

    private static PaymentRecord MapPayment(OracleDataReader reader) => new()
    {
        PaymentId = Convert.ToInt32(reader["payment_id"]),
        TaskId = Convert.ToInt32(reader["task_id"]),
        RecordId = Convert.ToInt32(reader["record_id"]),
        PublisherUserId = Convert.ToInt32(reader["publisher_user_id"]),
        OrderAmount = Convert.ToDecimal(reader["order_amount"]),
        PayAmount = Convert.ToDecimal(reader["pay_amount"]),
        PayMethod = Convert.ToString(reader["pay_method"]) ?? "CASH",
        ThirdTradeNo = reader["third_trade_no"] == DBNull.Value ? null : Convert.ToString(reader["third_trade_no"]),
        PayStatus = Convert.ToString(reader["pay_status"]) ?? "UNPAID"
    };

    private static PaymentListRecord MapPaymentList(OracleDataReader reader) => new()
    {
        PaymentId = Convert.ToInt32(reader["payment_id"]),
        TaskId = Convert.ToInt32(reader["task_id"]),
        RecordId = Convert.ToInt32(reader["record_id"]),
        TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
        OrderAmount = Convert.ToDecimal(reader["order_amount"]),
        PayAmount = Convert.ToDecimal(reader["pay_amount"]),
        PayMethod = Convert.ToString(reader["pay_method"]) ?? "CASH",
        ThirdTradeNo = reader["third_trade_no"] == DBNull.Value ? null : Convert.ToString(reader["third_trade_no"]),
        PayStatus = Convert.ToString(reader["pay_status"]) ?? "UNPAID",
        RefundProcessStatus = reader["refund_process_status"] == DBNull.Value ? null : Convert.ToString(reader["refund_process_status"]),
        IsSettled = Convert.ToInt32(reader["is_settled"]) == 1
    };
}
