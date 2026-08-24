using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class AuditRepository(OracleConnectionFactory connectionFactory) : IAuditRepository
{
    public async Task<IReadOnlyList<AuditLogRecord>> GetRecentAuditsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<AuditLogRecord>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT a.audit_id,
                   a.audit_object,
                   a.audit_result,
                   a.audited_at,
                   a.exception_note,
                   CASE a.audit_object
                       WHEN 'PAYMENT' THEN NVL(ap.cnt, 0)
                       WHEN 'REFUND' THEN NVL(ar.cnt, 0)
                       WHEN 'LOG' THEN NVL(al.cnt, 0)
                       ELSE 0
                   END AS related_count
            FROM APPUSER.audit_logs a
            LEFT JOIN (
                SELECT audit_id, COUNT(*) AS cnt
                FROM APPUSER.audit_payment_checks
                GROUP BY audit_id
            ) ap ON ap.audit_id = a.audit_id
            LEFT JOIN (
                SELECT audit_id, COUNT(*) AS cnt
                FROM APPUSER.audit_refund_checks
                GROUP BY audit_id
            ) ar ON ar.audit_id = a.audit_id
            LEFT JOIN (
                SELECT audit_id, COUNT(*) AS cnt
                FROM APPUSER.audit_status_log_checks
                GROUP BY audit_id
            ) al ON al.audit_id = a.audit_id
            ORDER BY a.audited_at DESC, a.audit_id DESC
            FETCH FIRST 100 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapAuditLog(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<AuditTargetRecord>> GetPaymentTargetsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<AuditTargetRecord>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT p.payment_id, p.pay_amount, p.pay_status, p.pay_method,
                   ar.task_id, t.task_title
            FROM APPUSER.payments p
            JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
            JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            ORDER BY p.payment_id DESC
            FETCH FIRST 80 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AuditTargetRecord
            {
                TargetId = Convert.ToInt32(reader["payment_id"]),
                Title = $"支付 #{Convert.ToInt32(reader["payment_id"])}",
                SecondaryText = $"任务 #{Convert.ToInt32(reader["task_id"])} · {Convert.ToString(reader["task_title"])} · {Convert.ToString(reader["pay_method"])}",
                Amount = Convert.ToDecimal(reader["pay_amount"]),
                Status = Convert.ToString(reader["pay_status"]) ?? string.Empty
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<AuditTargetRecord>> GetRefundTargetsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<AuditTargetRecord>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT rf.refund_id, rf.payment_id, rf.refund_amount, rf.refund_reason,
                   rf.process_status
            FROM APPUSER.refunds rf
            ORDER BY rf.refund_id DESC
            FETCH FIRST 80 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AuditTargetRecord
            {
                TargetId = Convert.ToInt32(reader["refund_id"]),
                Title = $"退款 #{Convert.ToInt32(reader["refund_id"])}",
                SecondaryText = $"支付 #{Convert.ToInt32(reader["payment_id"])} · {Convert.ToString(reader["refund_reason"])}",
                Amount = Convert.ToDecimal(reader["refund_amount"]),
                Status = Convert.ToString(reader["process_status"]) ?? string.Empty
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<AuditTargetRecord>> GetStatusLogTargetsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<AuditTargetRecord>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT l.log_id, l.record_id, l.status_before, l.status_after,
                   l.operator_user_id, l.operated_at, ar.task_id, t.task_title
            FROM APPUSER.task_status_logs l
            JOIN APPUSER.assign_records ar ON ar.record_id = l.record_id
            JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            ORDER BY l.operated_at DESC, l.log_id DESC
            FETCH FIRST 80 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            string statusBefore = reader["status_before"] == DBNull.Value ? "无" : Convert.ToString(reader["status_before"]) ?? "无";
            string statusAfter = Convert.ToString(reader["status_after"]) ?? string.Empty;
            items.Add(new AuditTargetRecord
            {
                TargetId = Convert.ToInt32(reader["log_id"]),
                Title = $"状态日志 #{Convert.ToInt32(reader["log_id"])}",
                SecondaryText = $"任务 #{Convert.ToInt32(reader["task_id"])} · {Convert.ToString(reader["task_title"])} · {statusBefore} -> {statusAfter}",
                Status = statusAfter,
                OccurredAt = Convert.ToDateTime(reader["operated_at"])
            });
        }

        return items;
    }

    public async Task<int> GetTargetCountAsync(string auditObject, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = auditObject switch
        {
            "PAYMENT" => "SELECT COUNT(*) FROM APPUSER.payments",
            "REFUND" => "SELECT COUNT(*) FROM APPUSER.refunds",
            "LOG" => "SELECT COUNT(*) FROM APPUSER.task_status_logs",
            _ => "SELECT 0 FROM dual"
        };
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> InsertAuditLogAsync(
        AuditLogRecord record,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.audit_logs (audit_object, audit_result, audited_at, exception_note)
            VALUES (:auditObject, :auditResult, SYSDATE, :exceptionNote)
            RETURNING audit_id INTO :auditId
            """;
        command.Parameters.Add(new OracleParameter("auditObject", record.AuditObject));
        command.Parameters.Add(new OracleParameter("auditResult", record.AuditResult));
        command.Parameters.Add(new OracleParameter("exceptionNote", (object?)record.ExceptionNote ?? DBNull.Value));

        var idParameter = new OracleParameter("auditId", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return idParameter.Value is OracleDecimal oracleDecimal
            ? oracleDecimal.ToInt32()
            : Convert.ToInt32(idParameter.Value);
    }

    public async Task InsertAuditLinkAsync(
        int auditId,
        string auditObject,
        int targetId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = auditObject switch
        {
            "PAYMENT" => """
                INSERT INTO APPUSER.audit_payment_checks (audit_id, payment_id)
                VALUES (:auditId, :targetId)
                """,
            "REFUND" => """
                INSERT INTO APPUSER.audit_refund_checks (audit_id, refund_id)
                VALUES (:auditId, :targetId)
                """,
            "LOG" => """
                INSERT INTO APPUSER.audit_status_log_checks (audit_id, log_id)
                VALUES (:auditId, :targetId)
                """,
            _ => throw new InvalidOperationException("Unsupported audit object.")
        };
        command.Parameters.Add(new OracleParameter("auditId", auditId));
        command.Parameters.Add(new OracleParameter("targetId", targetId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static AuditLogRecord MapAuditLog(OracleDataReader reader)
    {
        return new AuditLogRecord
        {
            AuditId = Convert.ToInt32(reader["audit_id"]),
            AuditObject = Convert.ToString(reader["audit_object"]) ?? "PAYMENT",
            AuditResult = Convert.ToString(reader["audit_result"]) ?? "PASS",
            AuditedAt = Convert.ToDateTime(reader["audited_at"]),
            ExceptionNote = reader["exception_note"] == DBNull.Value ? null : Convert.ToString(reader["exception_note"]),
            RelatedCount = Convert.ToInt32(reader["related_count"])
        };
    }
}

