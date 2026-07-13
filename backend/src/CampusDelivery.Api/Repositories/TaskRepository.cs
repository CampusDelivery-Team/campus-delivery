using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;


namespace CampusDelivery.Api.Repositories;

public sealed class TaskRepository(OracleConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<CampusTask>> GetGrabableTasksAsync(int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var tasks = new List<CampusTask>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT task_id, publisher_user_id, service_type_id, address_no, node_id, 
                   task_title, task_price, urgent_flag, task_status, created_at, completed_at
            FROM APPUSER.tasks
            WHERE task_status = 'WAITING'
            ORDER BY urgent_flag DESC, created_at DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tasks.Add(MapTask(reader));
        }
        return tasks;
    }

    public async Task<int> GetGrabableCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.tasks WHERE task_status = 'WAITING'";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<Runner?> GetRunnerByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT runner_id, user_id, real_name, identity_info, audit_status, work_status, credit_score
            FROM APPUSER.runners
            WHERE user_id = :userId
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapRunner(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<CampusTask>> GetActiveTasksByRunnerIdAsync(int runnerId, CancellationToken cancellationToken = default)
    {
        var tasks = new List<CampusTask>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT t.task_id, t.publisher_user_id, t.service_type_id, t.address_no, t.node_id,
                   t.task_title, t.task_price, t.urgent_flag, t.task_status, t.created_at, t.completed_at
            FROM APPUSER.tasks t
            JOIN APPUSER.assign_records r ON t.task_id = r.task_id
            WHERE r.runner_id = :runnerId
              AND r.record_id = (
                  SELECT MAX(r2.record_id)
                  FROM APPUSER.assign_records r2
                  WHERE r2.task_id = t.task_id
              )
              AND t.task_status IN ('ASSIGNED', 'PICKED_UP', 'DELIVERING', 'WAIT_CONFIRM')
            ORDER BY t.created_at DESC

            """;
        command.Parameters.Add(new OracleParameter("runnerId", runnerId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tasks.Add(MapTask(reader));
        }
        return tasks;
    }

    public async Task<IReadOnlyList<CampusTask>> GetTasksWaitingForReceiptAsync(int publisherUserId, CancellationToken cancellationToken = default)
    {
        var tasks = new List<CampusTask>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT t.task_id, t.publisher_user_id, t.service_type_id, t.address_no, t.node_id,
                   t.task_title, t.task_price, t.urgent_flag, t.task_status, t.created_at, t.completed_at
            FROM APPUSER.tasks t
            JOIN APPUSER.assign_records ar ON ar.record_id = (
                SELECT MAX(ar2.record_id)
                FROM APPUSER.assign_records ar2
                WHERE ar2.task_id = t.task_id
            )
            WHERE t.publisher_user_id = :publisherUserId
              AND t.task_status = 'WAIT_CONFIRM'
              AND NOT EXISTS (
                  SELECT 1
                  FROM APPUSER.task_status_logs l
                  WHERE l.record_id = ar.record_id
                    AND l.status_before = 'WAIT_CONFIRM'
                    AND l.status_after = 'WAIT_CONFIRM'
                    AND l.operator_user_id = :publisherUserId
              )
            ORDER BY t.created_at DESC
            """;
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tasks.Add(MapTask(reader));
        }
        return tasks;
    }

    public async Task<string?> GetTaskStatusWithLockAsync(int taskId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)

    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "SELECT task_status FROM APPUSER.tasks WHERE task_id = :taskId FOR UPDATE";
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? null : Convert.ToString(result);
    }

    public async Task<Runner?> GetRunnerWithLockAsync(int runnerId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT runner_id, user_id, real_name, identity_info, audit_status, work_status, credit_score
            FROM APPUSER.runners
            WHERE runner_id = :runnerId
            FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("runnerId", runnerId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapRunner(reader);
        }
        return null;
    }

    public async Task<int?> GetTaskPublisherUserIdAsync(int taskId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "SELECT publisher_user_id FROM APPUSER.tasks WHERE task_id = :taskId";
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
    }

    public async Task<bool> IsReceiptConfirmedAsync(int recordId, int publisherUserId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*)
            FROM APPUSER.task_status_logs
            WHERE record_id = :recordId
              AND status_before = 'WAIT_CONFIRM'
              AND status_after = 'WAIT_CONFIRM'
              AND operator_user_id = :publisherUserId
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        command.Parameters.Add(new OracleParameter("publisherUserId", publisherUserId));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task UpdateTaskStatusAsync(int taskId, string status, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)

    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.tasks
            SET task_status = :status,
                completed_at = CASE WHEN :status = 'FINISHED' THEN SYSDATE ELSE completed_at END
            WHERE task_id = :taskId
            """;
        command.Parameters.Add(new OracleParameter("status", status));
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateRunnerWorkStatusAsync(int runnerId, string workStatus, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "UPDATE APPUSER.runners SET work_status = :workStatus WHERE runner_id = :runnerId";
        command.Parameters.Add(new OracleParameter("workStatus", workStatus));
        command.Parameters.Add(new OracleParameter("runnerId", runnerId));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> InsertAssignRecordAsync(AssignRecord record, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.assign_records (task_id, runner_id, operation_type, assigned_at, reassign_reason)
            VALUES (:taskId, :runnerId, :operationType, SYSDATE, :reassignReason)
            RETURNING record_id INTO :recordId
            """;
        command.Parameters.Add(new OracleParameter("taskId", record.TaskId));
        command.Parameters.Add(new OracleParameter("runnerId", record.RunnerId));
        command.Parameters.Add(new OracleParameter("operationType", record.OperationType));
        command.Parameters.Add(new OracleParameter("reassignReason", (object?)record.ReassignReason ?? DBNull.Value));

        var recordIdParam = new OracleParameter("recordId", OracleDbType.Decimal, ParameterDirection.Output);
        command.Parameters.Add(recordIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return int.Parse(recordIdParam.Value.ToString()!);
    }

    public async Task InsertTaskStatusLogAsync(TaskStatusLog log, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.task_status_logs (record_id, status_before, status_after, operator_user_id, operated_at)
            VALUES (:recordId, :statusBefore, :statusAfter, :operatorUserId, SYSDATE)
            """;
        command.Parameters.Add(new OracleParameter("recordId", log.RecordId));
        command.Parameters.Add(new OracleParameter("statusBefore", (object?)log.StatusBefore ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("statusAfter", log.StatusAfter));
        command.Parameters.Add(new OracleParameter("operatorUserId", log.OperatorUserId));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AssignRecord?> GetLatestAssignRecordAsync(int taskId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT record_id, task_id, runner_id, operation_type, assigned_at, reassign_reason
            FROM APPUSER.assign_records
            WHERE task_id = :taskId
            ORDER BY assigned_at DESC, record_id DESC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapAssignRecord(reader);
        }
        return null;
    }

    public async Task<AssignRecord?> GetLatestAssignRecordWithConnectionAsync(int taskId, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT record_id, task_id, runner_id, operation_type, assigned_at, reassign_reason
            FROM APPUSER.assign_records
            WHERE task_id = :taskId
            ORDER BY assigned_at DESC, record_id DESC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("taskId", taskId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapAssignRecord(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<CampusTask>> GetWaitingTasksForAdminAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new List<CampusTask>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT task_id, publisher_user_id, service_type_id, address_no, node_id,
                   task_title, task_price, urgent_flag, task_status, created_at, completed_at
            FROM APPUSER.tasks
            WHERE task_status = 'WAITING'
            ORDER BY created_at DESC
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tasks.Add(MapTask(reader));
        }
        return tasks;
    }

    public async Task<IReadOnlyList<Runner>> GetFreeRunnersForAdminAsync(CancellationToken cancellationToken = default)
    {
        var runners = new List<Runner>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT runner_id, user_id, real_name, identity_info, audit_status, work_status, credit_score
            FROM APPUSER.runners
            WHERE audit_status = 'APPROVED' AND work_status = 'FREE'
            ORDER BY runner_id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            runners.Add(MapRunner(reader));
        }
        return runners;
    }



    public async Task<IReadOnlyList<TaskStatusLog>> GetStatusLogsByRecordIdAsync(int recordId, CancellationToken cancellationToken = default)
    {
        var logs = new List<TaskStatusLog>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT log_id, record_id, status_before, status_after, operator_user_id, operated_at
            FROM APPUSER.task_status_logs
            WHERE record_id = :recordId
            ORDER BY operated_at ASC, log_id ASC
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            logs.Add(new TaskStatusLog
            {
                LogId = Convert.ToInt32(reader["log_id"]),
                RecordId = Convert.ToInt32(reader["record_id"]),
                StatusBefore = reader["status_before"] == DBNull.Value ? null : Convert.ToString(reader["status_before"]),
                StatusAfter = Convert.ToString(reader["status_after"]) ?? string.Empty,
                OperatorUserId = Convert.ToInt32(reader["operator_user_id"]),
                OperatedAt = Convert.ToDateTime(reader["operated_at"])
            });
        }
        return logs;
    }

    // 辅助查询：获取服务名称、节点名称、以及地址格式
    public async Task<string> GetServiceTypeNameAsync(int serviceTypeId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = "SELECT service_name FROM APPUSER.service_types WHERE service_type_id = :id";
        command.Parameters.Add(new OracleParameter("id", serviceTypeId));
        var res = await command.ExecuteScalarAsync(cancellationToken);
        return res == DBNull.Value ? string.Empty : Convert.ToString(res) ?? string.Empty;
    }

    public async Task<string> GetNodeNameAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = "SELECT node_name FROM APPUSER.nodes WHERE node_id = :id";
        command.Parameters.Add(new OracleParameter("id", nodeId));
        var res = await command.ExecuteScalarAsync(cancellationToken);
        return res == DBNull.Value ? string.Empty : Convert.ToString(res) ?? string.Empty;
    }

    public async Task<(string ContactName, string ContactPhone, string AddressDisplay)> GetAddressDetailsAsync(int userId, int addressNo, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT contact_name, contact_phone, campus, building_room
            FROM APPUSER.user_addresses
            WHERE user_id = :userId AND address_no = :addressNo
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));
        command.Parameters.Add(new OracleParameter("addressNo", addressNo));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            string campus = Convert.ToString(reader["campus"]) ?? string.Empty;
            string room = Convert.ToString(reader["building_room"]) ?? string.Empty;
            return (
                Convert.ToString(reader["contact_name"]) ?? string.Empty,
                Convert.ToString(reader["contact_phone"]) ?? string.Empty,
                $"{campus} {room}".Trim()
            );
        }
        return (string.Empty, string.Empty, string.Empty);
    }

    public async Task<string> GetUsernameByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = "SELECT username FROM APPUSER.users WHERE user_id = :id";
        command.Parameters.Add(new OracleParameter("id", userId));
        var res = await command.ExecuteScalarAsync(cancellationToken);
        return res == DBNull.Value ? string.Empty : Convert.ToString(res) ?? string.Empty;
    }



    private static CampusTask MapTask(OracleDataReader reader)
    {
        return new CampusTask
        {
            TaskId = Convert.ToInt32(reader["task_id"]),
            PublisherUserId = Convert.ToInt32(reader["publisher_user_id"]),
            ServiceTypeId = Convert.ToInt32(reader["service_type_id"]),
            AddressNo = Convert.ToInt32(reader["address_no"]),
            NodeId = Convert.ToInt32(reader["node_id"]),
            TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
            TaskPrice = Convert.ToDecimal(reader["task_price"]),
            UrgentFlag = Convert.ToString(reader["urgent_flag"]) ?? "N",
            TaskStatus = Convert.ToString(reader["task_status"]) ?? "CREATED",
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            CompletedAt = reader["completed_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["completed_at"])
        };
    }

    private static Runner MapRunner(OracleDataReader reader)
    {
        return new Runner
        {
            RunnerId = Convert.ToInt32(reader["runner_id"]),
            UserId = Convert.ToInt32(reader["user_id"]),
            RealName = Convert.ToString(reader["real_name"]) ?? string.Empty,
            IdentityInfo = Convert.ToString(reader["identity_info"]) ?? string.Empty,
            AuditStatus = Convert.ToString(reader["audit_status"]) ?? "PENDING",
            WorkStatus = Convert.ToString(reader["work_status"]) ?? "OFFLINE",
            CreditScore = Convert.ToInt32(reader["credit_score"])
        };
    }

    private static AssignRecord MapAssignRecord(OracleDataReader reader)
    {
        return new AssignRecord
        {
            RecordId = Convert.ToInt32(reader["record_id"]),
            TaskId = Convert.ToInt32(reader["task_id"]),
            RunnerId = Convert.ToInt32(reader["runner_id"]),
            OperationType = Convert.ToString(reader["operation_type"]) ?? "SELF",
            AssignedAt = Convert.ToDateTime(reader["assigned_at"]),
            ReassignReason = reader["reassign_reason"] == DBNull.Value ? null : Convert.ToString(reader["reassign_reason"])
        };
    }
}
