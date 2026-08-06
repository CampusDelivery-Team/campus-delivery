using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class ReportRepository(OracleConnectionFactory connectionFactory)
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
            SELECT r.runner_id, r.real_name,
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
            GROUP BY r.runner_id, r.real_name
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
                SettledIncome = Convert.ToDecimal(reader["settled_income"])
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

    public async Task<int> InsertReportAsync(
        ReportRecord report,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
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
}

