using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class ReportRepository(OracleConnectionFactory connectionFactory) : IReportRepository
{
    public async Task<IReadOnlyList<ReportMetricRecord>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var metrics = new List<ReportMetricRecord>
        {
            new()
            {
                Name = "任务总数",
                Value = (await GetScalarAsync(connection, "SELECT COUNT(*) FROM APPUSER.tasks", cancellationToken)).ToString("N0"),
                Note = "系统累计发布任务"
            },
            new()
            {
                Name = "已完成任务",
                Value = (await GetScalarAsync(connection, "SELECT COUNT(*) FROM APPUSER.tasks WHERE task_status = 'FINISHED'", cancellationToken)).ToString("N0"),
                Note = "已收货并完成支付"
            },
            new()
            {
                Name = "已支付金额",
                Value = $"¥{(await GetDecimalAsync(connection, "SELECT NVL(SUM(pay_amount), 0) FROM APPUSER.payments WHERE pay_status = 'PAID'", cancellationToken)):F2}",
                Note = "可作为结算收入来源"
            },
            new()
            {
                Name = "已结算净收入",
                Value = $"¥{(await GetDecimalAsync(connection, "SELECT NVL(SUM(net_income), 0) FROM APPUSER.settlements WHERE settlement_status IN ('WAITING', 'DONE')", cancellationToken)):F2}",
                Note = "已生成结算单的跑腿员收入"
            },
            new()
            {
                Name = "退款记录",
                Value = (await GetScalarAsync(connection, "SELECT COUNT(*) FROM APPUSER.refunds", cancellationToken)).ToString("N0"),
                Note = "全部退款申请"
            },
            new()
            {
                Name = "未处理投诉",
                Value = (await GetScalarAsync(connection, "SELECT COUNT(*) FROM APPUSER.complaints WHERE process_status IN ('SUBMITTED', 'PROCESSING')", cancellationToken)).ToString("N0"),
                Note = "会阻断普通结算"
            }
        };

        return metrics;
    }

    public async Task<IReadOnlyList<NodeVolumeRecord>> GetNodeVolumesAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<NodeVolumeRecord>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT n.node_id, n.node_name,
                   COUNT(t.task_id) AS task_count,
                   SUM(CASE WHEN t.task_status = 'FINISHED' THEN 1 ELSE 0 END) AS finished_count
            FROM APPUSER.nodes n
            LEFT JOIN APPUSER.tasks t ON t.node_id = n.node_id
            GROUP BY n.node_id, n.node_name
            ORDER BY task_count DESC, n.node_id
            FETCH FIRST 10 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NodeVolumeRecord
            {
                NodeId = Convert.ToInt32(reader["node_id"]),
                NodeName = Convert.ToString(reader["node_name"]) ?? string.Empty,
                TaskCount = Convert.ToInt32(reader["task_count"]),
                FinishedTaskCount = Convert.ToInt32(reader["finished_count"])
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<RunnerPerformanceRecord>> GetRunnerPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<RunnerPerformanceRecord>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT r.runner_id, r.real_name, r.credit_score,
                   COUNT(DISTINCT CASE WHEN t.task_status = 'FINISHED' THEN t.task_id END) AS finished_count,
                   NVL(SUM(CASE WHEN p.pay_status = 'PAID' THEN p.pay_amount ELSE 0 END), 0) AS paid_amount,
                   NVL((SELECT SUM(s.net_income)
                        FROM APPUSER.settlements s
                        WHERE s.runner_id = r.runner_id
                          AND s.settlement_status IN ('WAITING', 'DONE')), 0) AS settled_income
            FROM APPUSER.runners r
            LEFT JOIN APPUSER.assign_records ar ON ar.runner_id = r.runner_id
            LEFT JOIN APPUSER.tasks t ON t.task_id = ar.task_id
            LEFT JOIN APPUSER.payments p ON p.record_id = ar.record_id
            GROUP BY r.runner_id, r.real_name, r.credit_score
            ORDER BY finished_count DESC, paid_amount DESC, r.runner_id
            FETCH FIRST 10 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new RunnerPerformanceRecord
            {
                RunnerId = Convert.ToInt32(reader["runner_id"]),
                RunnerName = Convert.ToString(reader["real_name"]) ?? string.Empty,
                FinishedTaskCount = Convert.ToInt32(reader["finished_count"]),
                PaidAmount = Convert.ToDecimal(reader["paid_amount"]),
                SettledIncome = Convert.ToDecimal(reader["settled_income"]),
                CreditScore = Convert.ToDecimal(reader["credit_score"])
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<ReportRecord>> GetRecentReportsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<ReportRecord>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT report_id, report_type, stat_period, generated_at, report_status
            FROM APPUSER.reports
            ORDER BY generated_at DESC, report_id DESC
            FETCH FIRST 30 ROWS ONLY
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapReport(reader));
        }

        return items;
    }

    public async Task<ReportRecord?> GetByIdAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT report_id, report_type, stat_period, generated_at, report_status
              FROM APPUSER.reports
             WHERE report_id = :reportId
            """;
        command.Parameters.Add(new OracleParameter("reportId", reportId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReport(reader) : null;
    }

    public async Task<IReadOnlyList<ReportBusinessItem>> GetBusinessItemsAsync(
        string reportType,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        string sql = reportType switch
        {
            "ORDER" => """
                SELECT t.task_id AS business_id,
                       t.task_id,
                       t.task_title,
                       t.task_status AS primary_status,
                       CAST(NULL AS VARCHAR2(20 CHAR)) AS secondary_status,
                       t.task_price AS amount,
                       t.created_at AS occurred_at,
                       st.service_name || ' · ' || n.node_name AS description
                  FROM APPUSER.tasks t
                  JOIN APPUSER.service_types st ON st.service_type_id = t.service_type_id
                  JOIN APPUSER.nodes n ON n.node_id = t.node_id
                 WHERE t.created_at >= :periodStart
                   AND t.created_at < :periodEnd
                 ORDER BY t.created_at, t.task_id
                """,
            "PAYMENT" => """
                SELECT p.payment_id AS business_id,
                       ar.task_id,
                       t.task_title,
                       p.pay_status AS primary_status,
                       latest_refund.process_status AS secondary_status,
                       p.pay_amount AS amount,
                       NVL(t.completed_at, t.created_at) AS occurred_at,
                       p.pay_method AS description
                  FROM APPUSER.payments p
                  JOIN APPUSER.assign_records ar ON ar.record_id = p.record_id
                  JOIN APPUSER.tasks t ON t.task_id = ar.task_id
                  LEFT JOIN (
                      SELECT payment_id, process_status,
                             ROW_NUMBER() OVER (PARTITION BY payment_id ORDER BY refund_id DESC) AS rn
                        FROM APPUSER.refunds
                  ) latest_refund ON latest_refund.payment_id = p.payment_id
                                 AND latest_refund.rn = 1
                 WHERE NVL(t.completed_at, t.created_at) >= :periodStart
                   AND NVL(t.completed_at, t.created_at) < :periodEnd
                 ORDER BY occurred_at, p.payment_id
                """,
            "COMPLAINT" => """
                SELECT c.complaint_id AS business_id,
                       ar.task_id,
                       t.task_title,
                       c.process_status AS primary_status,
                       CAST(NULL AS VARCHAR2(20 CHAR)) AS secondary_status,
                       0 AS amount,
                       t.created_at AS occurred_at,
                       c.reason AS description
                  FROM APPUSER.complaints c
                  JOIN APPUSER.assign_records ar ON ar.record_id = c.record_id
                  JOIN APPUSER.tasks t ON t.task_id = ar.task_id
                 WHERE t.created_at >= :periodStart
                   AND t.created_at < :periodEnd
                 ORDER BY t.created_at, c.complaint_id
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(reportType), reportType, "不支持的报表类型")
        };

        var items = new List<ReportBusinessItem>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = sql;
        command.Parameters.Add(new OracleParameter("periodStart", periodStart));
        command.Parameters.Add(new OracleParameter("periodEnd", periodEnd));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapBusinessItem(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<int>> GetAuditIdsForReportAsync(
        string reportType,
        DateTime periodStart,
        DateTime periodEnd,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        var auditIds = new List<int>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT audit_id
              FROM APPUSER.audit_logs
             WHERE audited_at >= :periodStart
               AND audited_at < :periodEnd
               AND (
                   (:reportType = 'ORDER' AND audit_object = 'LOG')
                   OR (:reportType = 'PAYMENT' AND audit_object IN ('PAYMENT', 'REFUND'))
               )
             ORDER BY audit_id
            """;
        command.Parameters.Add(new OracleParameter("periodStart", periodStart));
        command.Parameters.Add(new OracleParameter("periodEnd", periodEnd));
        command.Parameters.Add(new OracleParameter("reportType", reportType));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            auditIds.Add(Convert.ToInt32(reader["audit_id"]));
        }

        return auditIds;
    }

    public async Task<IReadOnlyList<ReportAuditItem>> GetReportAuditItemsAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        var items = new List<ReportAuditItem>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT a.audit_id, a.audit_object, a.audit_result, a.audited_at, a.exception_note
              FROM APPUSER.report_audit_items rai
              JOIN APPUSER.audit_logs a ON a.audit_id = rai.audit_id
             WHERE rai.report_id = :reportId
             ORDER BY a.audited_at, a.audit_id
            """;
        command.Parameters.Add(new OracleParameter("reportId", reportId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ReportAuditItem
            {
                AuditId = Convert.ToInt32(reader["audit_id"]),
                AuditObject = Convert.ToString(reader["audit_object"]) ?? string.Empty,
                AuditResult = Convert.ToString(reader["audit_result"]) ?? string.Empty,
                AuditedAt = Convert.ToDateTime(reader["audited_at"]),
                ExceptionNote = reader["exception_note"] == DBNull.Value
                    ? null
                    : Convert.ToString(reader["exception_note"])
            });
        }

        return items;
    }

    public async Task<int> InsertReportAsync(
        ReportRecord report,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.reports (report_type, stat_period, generated_at, report_status)
            VALUES (:reportType, :statPeriod, SYSDATE, :reportStatus)
            RETURNING report_id INTO :reportId
            """;
        command.Parameters.Add(new OracleParameter("reportType", report.ReportType));
        command.Parameters.Add(new OracleParameter("statPeriod", report.StatPeriod));
        command.Parameters.Add(new OracleParameter("reportStatus", report.ReportStatus));

        var idParameter = new OracleParameter("reportId", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return idParameter.Value is OracleDecimal oracleDecimal
            ? oracleDecimal.ToInt32()
            : Convert.ToInt32(idParameter.Value);
    }

    public async Task InsertReportAuditItemAsync(
        int reportId,
        int auditId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.report_audit_items (report_id, audit_id)
            VALUES (:reportId, :auditId)
            """;
        command.Parameters.Add(new OracleParameter("reportId", reportId));
        command.Parameters.Add(new OracleParameter("auditId", auditId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(
        int reportId,
        string reportStatus,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.reports
               SET report_status = :reportStatus
             WHERE report_id = :reportId
            """;
        command.Parameters.Add(new OracleParameter("reportStatus", reportStatus));
        command.Parameters.Add(new OracleParameter("reportId", reportId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteAsync(
        int reportId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();

        await using var deleteItemsCommand = connection.CreateCommand();
        deleteItemsCommand.Transaction = transaction;
        deleteItemsCommand.BindByName = true;
        deleteItemsCommand.CommandText = """
            DELETE FROM APPUSER.report_audit_items
            WHERE report_id = :reportId
            """;
        deleteItemsCommand.Parameters.Add(new OracleParameter("reportId", reportId));
        await deleteItemsCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var deleteReportCommand = connection.CreateCommand();
        deleteReportCommand.Transaction = transaction;
        deleteReportCommand.BindByName = true;
        deleteReportCommand.CommandText = """
            DELETE FROM APPUSER.reports
            WHERE report_id = :reportId
            """;
        deleteReportCommand.Parameters.Add(new OracleParameter("reportId", reportId));
        return await deleteReportCommand.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static async Task<int> GetScalarAsync(
        OracleConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<decimal> GetDecimalAsync(
        OracleConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDecimal(result);
    }

    private static ReportRecord MapReport(OracleDataReader reader)
    {
        return new ReportRecord
        {
            ReportId = Convert.ToInt32(reader["report_id"]),
            ReportType = Convert.ToString(reader["report_type"]) ?? "ORDER",
            StatPeriod = Convert.ToString(reader["stat_period"]) ?? string.Empty,
            GeneratedAt = Convert.ToDateTime(reader["generated_at"]),
            ReportStatus = Convert.ToString(reader["report_status"]) ?? "GENERATED"
        };
    }

    private static ReportBusinessItem MapBusinessItem(OracleDataReader reader) => new()
    {
        BusinessId = Convert.ToInt32(reader["business_id"]),
        TaskId = Convert.ToInt32(reader["task_id"]),
        TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
        PrimaryStatus = Convert.ToString(reader["primary_status"]) ?? string.Empty,
        SecondaryStatus = reader["secondary_status"] == DBNull.Value
            ? null
            : Convert.ToString(reader["secondary_status"]),
        Amount = Convert.ToDecimal(reader["amount"]),
        OccurredAt = Convert.ToDateTime(reader["occurred_at"]),
        Description = Convert.ToString(reader["description"]) ?? string.Empty
    };
}
