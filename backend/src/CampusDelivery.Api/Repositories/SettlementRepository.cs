using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class SettlementRepository(OracleConnectionFactory connectionFactory)
{
    private const string SettlementCandidateFilterSql = """
              p.pay_status = 'PAID'
              AND t.task_status = 'FINISHED'
              AND (:runnerId IS NULL OR ar.runner_id = :runnerId)
              AND NOT EXISTS (
                  SELECT 1
                  FROM APPUSER.settlement_payment_items spi
                  WHERE spi.payment_id = p.payment_id
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM APPUSER.complaints c
                  WHERE c.record_id = p.record_id
                    AND c.process_status IN ('SUBMITTED', 'PROCESSING')
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM APPUSER.refunds rf
                  WHERE rf.payment_id = p.payment_id
                    AND rf.process_status IN ('APPLY', 'APPROVED', 'DONE')
              )
            """;

    public async Task<IReadOnlyList<Settlement>> GetRecentSettlementsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<Settlement>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT s.settlement_id, s.runner_id, r.real_name, s.order_total,
                   s.platform_fee, s.net_income, s.settlement_status
            FROM APPUSER.settlements s
            JOIN APPUSER.runners r ON r.runner_id = s.runner_id
            ORDER BY s.settlement_id DESC
            FETCH FIRST 80 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapSettlement(reader));
        }

        return items;
    }

    public async Task<SettlementCandidateSummary> GetSettlementCandidateSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = $"""
            SELECT COUNT(*) AS payment_count,
                   NVL(SUM(p.pay_amount), 0) AS pay_amount
            FROM APPUSER.payments p
            JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
            JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            WHERE {SettlementCandidateFilterSql}
            """;
        command.Parameters.Add(new OracleParameter("runnerId", OracleDbType.Int32)
        {
            Value = DBNull.Value
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new SettlementCandidateSummary();
        }

        return new SettlementCandidateSummary
        {
            PaymentCount = Convert.ToInt32(reader["payment_count"]),
            PayAmount = Convert.ToDecimal(reader["pay_amount"])
        };
    }

    public async Task<IReadOnlyList<SettlementCandidate>> GetSettlementCandidatesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetSettlementCandidatesAsync(connection, null, null, false, cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementCandidate>> GetSettlementCandidatesForRunnerWithLockAsync(
        int runnerId,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return await GetSettlementCandidatesAsync(connection, transaction, runnerId, true, cancellationToken);
    }

    public async Task<Settlement?> GetByIdAsync(int settlementId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT s.settlement_id, s.runner_id, r.real_name, s.order_total,
                   s.platform_fee, s.net_income, s.settlement_status
            FROM APPUSER.settlements s
            JOIN APPUSER.runners r ON r.runner_id = s.runner_id
            WHERE s.settlement_id = :settlementId
            """;
        command.Parameters.Add(new OracleParameter("settlementId", settlementId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapSettlement(reader) : null;
    }

    public async Task<RunnerSettlementSummary> GetRunnerSettlementSummaryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*) AS settlement_count,
                   NVL(SUM(CASE WHEN s.settlement_status = 'WAITING' THEN 1 ELSE 0 END), 0) AS waiting_count,
                   NVL(SUM(CASE WHEN s.settlement_status = 'DONE' THEN 1 ELSE 0 END), 0) AS done_count,
                   NVL(SUM(CASE WHEN s.settlement_status = 'BLOCKED' THEN 1 ELSE 0 END), 0) AS blocked_count,
                   NVL(SUM(s.net_income), 0) AS total_net_income,
                   NVL(SUM(CASE WHEN s.settlement_status = 'WAITING' THEN s.net_income ELSE 0 END), 0) AS waiting_net_income,
                   NVL(SUM(CASE WHEN s.settlement_status = 'DONE' THEN s.net_income ELSE 0 END), 0) AS done_net_income
            FROM APPUSER.settlements s
            JOIN APPUSER.runners r ON r.runner_id = s.runner_id
            WHERE r.user_id = :userId
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new RunnerSettlementSummary();
        }

        return new RunnerSettlementSummary
        {
            SettlementCount = Convert.ToInt32(reader["settlement_count"]),
            WaitingCount = Convert.ToInt32(reader["waiting_count"]),
            DoneCount = Convert.ToInt32(reader["done_count"]),
            BlockedCount = Convert.ToInt32(reader["blocked_count"]),
            TotalNetIncome = Convert.ToDecimal(reader["total_net_income"]),
            WaitingNetIncome = Convert.ToDecimal(reader["waiting_net_income"]),
            DoneNetIncome = Convert.ToDecimal(reader["done_net_income"])
        };
    }

    public async Task<IReadOnlyList<Settlement>> GetSettlementsByRunnerUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var items = new List<Settlement>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT s.settlement_id, s.runner_id, r.real_name, s.order_total,
                   s.platform_fee, s.net_income, s.settlement_status
            FROM APPUSER.settlements s
            JOIN APPUSER.runners r ON r.runner_id = s.runner_id
            WHERE r.user_id = :userId
            ORDER BY s.settlement_id DESC
            FETCH FIRST 80 ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapSettlement(reader));
        }

        return items;
    }

    public async Task<Settlement?> GetByIdForRunnerUserAsync(
        int settlementId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT s.settlement_id, s.runner_id, r.real_name, s.order_total,
                   s.platform_fee, s.net_income, s.settlement_status
            FROM APPUSER.settlements s
            JOIN APPUSER.runners r ON r.runner_id = s.runner_id
            WHERE s.settlement_id = :settlementId
              AND r.user_id = :userId
            """;
        command.Parameters.Add(new OracleParameter("settlementId", settlementId));
        command.Parameters.Add(new OracleParameter("userId", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapSettlement(reader) : null;
    }

    public async Task<IReadOnlyList<SettlementPaymentItem>> GetItemsAsync(
        int settlementId,
        CancellationToken cancellationToken = default)
    {
        var items = new List<SettlementPaymentItem>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT spi.settlement_id, spi.payment_id, p.record_id, ar.task_id,
                   t.task_title, p.pay_amount, p.pay_method
            FROM APPUSER.settlement_payment_items spi
            JOIN APPUSER.payments p ON p.payment_id = spi.payment_id
            JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
            JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            WHERE spi.settlement_id = :settlementId
            ORDER BY spi.payment_id
            """;
        command.Parameters.Add(new OracleParameter("settlementId", settlementId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SettlementPaymentItem
            {
                SettlementId = Convert.ToInt32(reader["settlement_id"]),
                PaymentId = Convert.ToInt32(reader["payment_id"]),
                RecordId = Convert.ToInt32(reader["record_id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
                PayAmount = Convert.ToDecimal(reader["pay_amount"]),
                PayMethod = Convert.ToString(reader["pay_method"]) ?? "CASH"
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<SettlementPaymentItem>> GetItemsForRunnerUserAsync(
        int settlementId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var items = new List<SettlementPaymentItem>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT spi.settlement_id, spi.payment_id, p.record_id, ar.task_id,
                   t.task_title, p.pay_amount, p.pay_method
            FROM APPUSER.settlement_payment_items spi
            JOIN APPUSER.settlements s ON s.settlement_id = spi.settlement_id
            JOIN APPUSER.runners r ON r.runner_id = s.runner_id
            JOIN APPUSER.payments p ON p.payment_id = spi.payment_id
            JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
            JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            WHERE spi.settlement_id = :settlementId
              AND r.user_id = :userId
            ORDER BY spi.payment_id
            """;
        command.Parameters.Add(new OracleParameter("settlementId", settlementId));
        command.Parameters.Add(new OracleParameter("userId", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SettlementPaymentItem
            {
                SettlementId = Convert.ToInt32(reader["settlement_id"]),
                PaymentId = Convert.ToInt32(reader["payment_id"]),
                RecordId = Convert.ToInt32(reader["record_id"]),
                TaskId = Convert.ToInt32(reader["task_id"]),
                TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
                PayAmount = Convert.ToDecimal(reader["pay_amount"]),
                PayMethod = Convert.ToString(reader["pay_method"]) ?? "CASH"
            });
        }

        return items;
    }

    public async Task<int> InsertSettlementAsync(
        Settlement settlement,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.settlements (
                runner_id, order_total, platform_fee, net_income, settlement_status
            ) VALUES (
                :runnerId, :orderTotal, :platformFee, :netIncome, :settlementStatus
            )
            RETURNING settlement_id INTO :settlementId
            """;
        command.Parameters.Add(new OracleParameter("runnerId", settlement.RunnerId));
        command.Parameters.Add(new OracleParameter("orderTotal", settlement.OrderTotal));
        command.Parameters.Add(new OracleParameter("platformFee", settlement.PlatformFee));
        command.Parameters.Add(new OracleParameter("netIncome", settlement.NetIncome));
        command.Parameters.Add(new OracleParameter("settlementStatus", settlement.SettlementStatus));

        var idParameter = new OracleParameter("settlementId", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return idParameter.Value is OracleDecimal oracleDecimal
            ? oracleDecimal.ToInt32()
            : Convert.ToInt32(idParameter.Value);
    }

    public async Task InsertSettlementItemAsync(
        int settlementId,
        int paymentId,
        OracleConnection connection,
        OracleTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.settlement_payment_items (settlement_id, payment_id)
            VALUES (:settlementId, :paymentId)
            """;
        command.Parameters.Add(new OracleParameter("settlementId", settlementId));
        command.Parameters.Add(new OracleParameter("paymentId", paymentId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        int settlementId,
        string status,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.settlements
            SET settlement_status = :status
            WHERE settlement_id = :settlementId
            """;
        command.Parameters.Add(new OracleParameter("status", status));
        command.Parameters.Add(new OracleParameter("settlementId", settlementId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<SettlementCandidate>> GetSettlementCandidatesAsync(
        OracleConnection connection,
        OracleTransaction? transaction,
        int? runnerId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        var items = new List<SettlementCandidate>();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = $"""
            SELECT p.payment_id, p.record_id, p.order_amount, p.pay_amount,
                   p.pay_method, p.pay_status, ar.task_id, ar.runner_id,
                   r.real_name, t.task_title, t.task_status
            FROM APPUSER.payments p
            JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
            JOIN APPUSER.runners r ON r.runner_id = ar.runner_id
            JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            WHERE {SettlementCandidateFilterSql}
            ORDER BY ar.runner_id, p.payment_id
            {(forUpdate ? "FOR UPDATE OF p.payment_id" : string.Empty)}
            """;
        command.Parameters.Add(new OracleParameter("runnerId", OracleDbType.Int32)
        {
            Value = (object?)runnerId ?? DBNull.Value
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SettlementCandidate
            {
                PaymentId = Convert.ToInt32(reader["payment_id"]),
                RecordId = Convert.ToInt32(reader["record_id"]),
                OrderAmount = Convert.ToDecimal(reader["order_amount"]),
                PayAmount = Convert.ToDecimal(reader["pay_amount"]),
                PayMethod = Convert.ToString(reader["pay_method"]) ?? "CASH",
                PayStatus = Convert.ToString(reader["pay_status"]) ?? "PAID",
                TaskId = Convert.ToInt32(reader["task_id"]),
                RunnerId = Convert.ToInt32(reader["runner_id"]),
                RunnerName = Convert.ToString(reader["real_name"]) ?? string.Empty,
                TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
                TaskStatus = Convert.ToString(reader["task_status"]) ?? "FINISHED"
            });
        }

        return items;
    }

    private static Settlement MapSettlement(OracleDataReader reader)
    {
        return new Settlement
        {
            SettlementId = Convert.ToInt32(reader["settlement_id"]),
            RunnerId = Convert.ToInt32(reader["runner_id"]),
            RunnerName = Convert.ToString(reader["real_name"]) ?? string.Empty,
            OrderTotal = Convert.ToDecimal(reader["order_total"]),
            PlatformFee = Convert.ToDecimal(reader["platform_fee"]),
            NetIncome = Convert.ToDecimal(reader["net_income"]),
            SettlementStatus = Convert.ToString(reader["settlement_status"]) ?? "WAITING"
        };
    }
}
