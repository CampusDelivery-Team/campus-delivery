using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CampusDelivery.Api.Repositories
{
    public sealed class TaskRepository
    {
        private readonly OracleConnectionFactory _connectionFactory;

        public TaskRepository(OracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<TaskCreateWriteResult> CreateAsync(
            TaskPublishRequest request,
            CancellationToken cancellationToken = default)
        {
            await using OracleConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using OracleTransaction transaction = connection.BeginTransaction();

            if (!await AddressExistsAsync(connection, transaction, request, cancellationToken))
            {
                transaction.Rollback();
                return new TaskCreateWriteResult(TaskCreateResult.AddressNotFound);
            }

            if (!await ServiceTypeAvailableAsync(connection, transaction, request.ServiceTypeId, cancellationToken))
            {
                transaction.Rollback();
                return new TaskCreateWriteResult(TaskCreateResult.ServiceTypeUnavailable);
            }

            if (!await NodeAvailableAsync(connection, transaction, request.NodeId, cancellationToken))
            {
                transaction.Rollback();
                return new TaskCreateWriteResult(TaskCreateResult.NodeUnavailable);
            }

            if (!await ServiceNodeRuleExistsAsync(connection, transaction, request, cancellationToken))
            {
                transaction.Rollback();
                return new TaskCreateWriteResult(TaskCreateResult.RuleNotMatched);
            }

            int taskId = await InsertTaskAsync(connection, transaction, request, cancellationToken);
            await InsertTaskDetailAsync(connection, transaction, taskId, request, cancellationToken);

            transaction.Commit();
            return new TaskCreateWriteResult(TaskCreateResult.Success, taskId);
        }

        public async Task<IReadOnlyList<TaskRecord>> GetListAsync(
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            List<TaskRecord> tasks = new List<TaskRecord>();

            await using OracleConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = $"""
                SELECT t.task_id,
                       st.service_name,
                       ua.contact_name,
                       ua.contact_phone,
                       ua.campus,
                       ua.building_room,
                       n.node_name,
                       t.task_title,
                       t.task_price,
                       t.urgent_flag,
                       t.task_status,
                       t.created_at,
                       CASE
                           WHEN f.task_id IS NOT NULL THEN 'FOOD'
                           WHEN e.task_id IS NOT NULL THEN 'EXPRESS'
                           WHEN p.task_id IS NOT NULL THEN 'PRIVATE'
                           ELSE 'UNKNOWN'
                       END AS task_kind
                FROM tasks t
                JOIN users u
                  ON u.user_id = t.publisher_user_id
                JOIN service_types st
                  ON st.service_type_id = t.service_type_id
                JOIN user_addresses ua
                  ON ua.user_id = t.publisher_user_id
                 AND ua.address_no = t.address_no
                JOIN nodes n
                  ON n.node_id = t.node_id
                LEFT JOIN food_delivery_details f
                  ON f.task_id = t.task_id
                 AND f.detail_no = 1
                LEFT JOIN express_pickup_details e
                  ON e.task_id = t.task_id
                 AND e.detail_no = 1
                LEFT JOIN private_task_details p
                  ON p.task_id = t.task_id
                 AND p.detail_no = 1
                WHERE t.publisher_user_id = :currentUserId
                ORDER BY t.created_at DESC, t.task_id DESC
                """;
            command.Parameters.Add(new OracleParameter("currentUserId", currentUserId));

            await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tasks.Add(new TaskRecord
                {
                    TaskId = Convert.ToInt32(reader["task_id"]),
                    ServiceName = Convert.ToString(reader["service_name"]) ?? string.Empty,
                    ContactName = Convert.ToString(reader["contact_name"]) ?? string.Empty,
                    ContactPhone = Convert.ToString(reader["contact_phone"]) ?? string.Empty,
                    Campus = Convert.ToString(reader["campus"]) ?? string.Empty,
                    BuildingRoom = Convert.ToString(reader["building_room"]) ?? string.Empty,
                    NodeName = Convert.ToString(reader["node_name"]) ?? string.Empty,
                    TaskTitle = Convert.ToString(reader["task_title"]) ?? string.Empty,
                    TaskPrice = Convert.ToDecimal(reader["task_price"]),
                    UrgentFlag = Convert.ToString(reader["urgent_flag"]) ?? "N",
                    TaskStatus = Convert.ToString(reader["task_status"]) ?? "WAITING",
                    CreatedAt = Convert.ToDateTime(reader["created_at"]),
                    TaskKind = Convert.ToString(reader["task_kind"]) ?? "UNKNOWN"
                });
            }

            return tasks;
        }

        public async Task<TaskCancelResult> CancelAsync(
            int taskId,
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            await using OracleConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = $"""
                UPDATE tasks
                SET task_status = 'CANCELLED'
                WHERE task_id = :taskId
                  AND task_status = 'WAITING'
                  AND publisher_user_id = :currentUserId
                """;
            command.Parameters.Add(new OracleParameter("taskId", taskId));
            command.Parameters.Add(new OracleParameter("currentUserId", currentUserId));

            if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
            {
                return TaskCancelResult.Success;
            }

            if (await ExistsAsync(taskId, currentUserId, cancellationToken))
            {
                return TaskCancelResult.InvalidState;
            }

            return TaskCancelResult.NotFound;
        }

        private async Task<bool> ExistsAsync(
            int taskId,
            int currentUserId,
            CancellationToken cancellationToken)
        {
            await using OracleConnection connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = $"""
                SELECT COUNT(*)
                FROM tasks
                WHERE task_id = :taskId
                  AND publisher_user_id = :currentUserId
                """;
            command.Parameters.Add(new OracleParameter("taskId", taskId));
            command.Parameters.Add(new OracleParameter("currentUserId", currentUserId));

            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }

        private static async Task<bool> AddressExistsAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            TaskPublishRequest request,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                SELECT COUNT(*)
                FROM user_addresses
                WHERE user_id = :publisherUserId
                  AND address_no = :addressNo
                """;
            command.Parameters.Add(new OracleParameter("publisherUserId", request.PublisherUserId));
            command.Parameters.Add(new OracleParameter("addressNo", request.AddressNo));

            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }

        private static async Task<bool> ServiceTypeAvailableAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            int serviceTypeId,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                SELECT COUNT(*)
                FROM service_types
                WHERE service_type_id = :serviceTypeId
                  AND type_status = 'ENABLED'
                """;
            command.Parameters.Add(new OracleParameter("serviceTypeId", serviceTypeId));

            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }

        private static async Task<bool> NodeAvailableAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            int nodeId,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                SELECT COUNT(*)
                FROM nodes
                WHERE node_id = :nodeId
                  AND node_status = 'NORMAL'
                """;
            command.Parameters.Add(new OracleParameter("nodeId", nodeId));

            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }

        private static async Task<bool> ServiceNodeRuleExistsAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            TaskPublishRequest request,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                SELECT COUNT(*)
                FROM service_node_rules
                WHERE service_type_id = :serviceTypeId
                  AND node_id = :nodeId
                """;
            command.Parameters.Add(new OracleParameter("serviceTypeId", request.ServiceTypeId));
            command.Parameters.Add(new OracleParameter("nodeId", request.NodeId));

            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }

        private static async Task<int> InsertTaskAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            TaskPublishRequest request,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO tasks (
                    publisher_user_id,
                    service_type_id,
                    address_no,
                    node_id,
                    task_title,
                    task_price,
                    urgent_flag,
                    task_status
                )
                VALUES (
                    :publisherUserId,
                    :serviceTypeId,
                    :addressNo,
                    :nodeId,
                    :taskTitle,
                    :taskPrice,
                    :urgentFlag,
                    'WAITING'
                )
                RETURNING task_id INTO :taskId
                """;
            command.Parameters.Add(new OracleParameter("publisherUserId", request.PublisherUserId));
            command.Parameters.Add(new OracleParameter("serviceTypeId", request.ServiceTypeId));
            command.Parameters.Add(new OracleParameter("addressNo", request.AddressNo));
            command.Parameters.Add(new OracleParameter("nodeId", request.NodeId));
            command.Parameters.Add(new OracleParameter("taskTitle", request.TaskTitle));
            command.Parameters.Add(new OracleParameter("taskPrice", request.TaskPrice));
            command.Parameters.Add(new OracleParameter("urgentFlag", request.UrgentFlag));

            OracleParameter taskIdParameter = new OracleParameter("taskId", OracleDbType.Int32);
            taskIdParameter.Direction = System.Data.ParameterDirection.Output;
            command.Parameters.Add(taskIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (taskIdParameter.Value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.ToInt32();
            }

            return Convert.ToInt32(taskIdParameter.Value);
        }

        private static async Task InsertTaskDetailAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            int taskId,
            TaskPublishRequest request,
            CancellationToken cancellationToken)
        {
            if (request.TaskKind == "FOOD")
            {
                await InsertFoodDetailAsync(connection, transaction, taskId, request, cancellationToken);
                return;
            }

            if (request.TaskKind == "EXPRESS")
            {
                await InsertExpressDetailAsync(connection, transaction, taskId, request, cancellationToken);
                return;
            }

            await InsertPrivateDetailAsync(connection, transaction, taskId, request, cancellationToken);
        }

        private static async Task InsertFoodDetailAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            int taskId,
            TaskPublishRequest request,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO food_delivery_details (
                    task_id,
                    detail_no,
                    merchant_name,
                    platform_order_no,
                    pickup_note
                )
                VALUES (
                    :taskId,
                    1,
                    :merchantName,
                    :platformOrderNo,
                    :pickupNote
                )
                """;
            command.Parameters.Add(new OracleParameter("taskId", taskId));
            command.Parameters.Add(new OracleParameter("merchantName", request.MerchantName));
            command.Parameters.Add(new OracleParameter(
                "platformOrderNo",
                (object?)request.PlatformOrderNo ?? DBNull.Value));
            command.Parameters.Add(new OracleParameter(
                "pickupNote",
                (object?)request.FoodPickupNote ?? DBNull.Value));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertExpressDetailAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            int taskId,
            TaskPublishRequest request,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO express_pickup_details (
                    task_id,
                    detail_no,
                    express_company,
                    waybill_no,
                    pickup_code,
                    pickup_note
                )
                VALUES (
                    :taskId,
                    1,
                    :expressCompany,
                    :waybillNo,
                    :pickupCode,
                    :pickupNote
                )
                """;
            command.Parameters.Add(new OracleParameter("taskId", taskId));
            command.Parameters.Add(new OracleParameter("expressCompany", request.ExpressCompany));
            command.Parameters.Add(new OracleParameter("waybillNo", request.WaybillNo));
            command.Parameters.Add(new OracleParameter("pickupCode", request.PickupCode));
            command.Parameters.Add(new OracleParameter(
                "pickupNote",
                (object?)request.ExpressPickupNote ?? DBNull.Value));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertPrivateDetailAsync(
            OracleConnection connection,
            OracleTransaction transaction,
            int taskId,
            TaskPublishRequest request,
            CancellationToken cancellationToken)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.BindByName = true;
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO private_task_details (
                    task_id,
                    detail_no,
                    item_category,
                    pickup_location,
                    delivery_location,
                    expected_finish_at,
                    description
                )
                VALUES (
                    :taskId,
                    1,
                    :itemCategory,
                    :pickupLocation,
                    :deliveryLocation,
                    :expectedFinishAt,
                    :description
                )
                """;
            command.Parameters.Add(new OracleParameter("taskId", taskId));
            command.Parameters.Add(new OracleParameter("itemCategory", request.ItemCategory));
            command.Parameters.Add(new OracleParameter("pickupLocation", request.PickupLocation));
            command.Parameters.Add(new OracleParameter("deliveryLocation", request.DeliveryLocation));
            command.Parameters.Add(new OracleParameter(
                "expectedFinishAt",
                (object?)request.ExpectedFinishAt ?? DBNull.Value));
            command.Parameters.Add(new OracleParameter(
                "description",
                (object?)request.PrivateDescription ?? DBNull.Value));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

public sealed class TaskCreateWriteResult
{
    public TaskCreateWriteResult(TaskCreateResult result, int taskId = 0)
    {
        Result = result;
        TaskId = taskId;
    }

    public TaskCreateResult Result { get; }

    public int TaskId { get; }
}

public enum TaskCreateResult
{
    Success,
    AddressNotFound,
    ServiceTypeUnavailable,
    NodeUnavailable,
    RuleNotMatched
}

public enum TaskCancelResult
{
    Success,
    NotFound,
    InvalidState
}
