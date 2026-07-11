using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class ServiceNodeRuleRepository(OracleConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<ServiceNodeRule>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var rules = new List<ServiceNodeRule>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.service_type_id,
                   st.service_name,
                   st.type_status,
                   r.node_id,
                   n.node_name,
                   n.node_type,
                   n.node_status,
                   CASE
                       WHEN EXISTS (
                           SELECT 1
                           FROM tasks t
                           WHERE t.service_type_id = r.service_type_id
                             AND t.node_id = r.node_id
                       ) THEN 1
                       ELSE 0
                   END AS is_referenced
            FROM service_node_rules r
            JOIN service_types st ON st.service_type_id = r.service_type_id
            JOIN nodes n ON n.node_id = r.node_id
            ORDER BY st.service_type_id, n.node_id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rules.Add(new ServiceNodeRule
            {
                ServiceTypeId = Convert.ToInt32(reader["service_type_id"]),
                ServiceName = Convert.ToString(reader["service_name"]) ?? string.Empty,
                ServiceTypeStatus = Convert.ToString(reader["type_status"]) ?? "ENABLED",
                NodeId = Convert.ToInt32(reader["node_id"]),
                NodeName = Convert.ToString(reader["node_name"]) ?? string.Empty,
                NodeType = Convert.ToString(reader["node_type"]) ?? string.Empty,
                NodeStatus = Convert.ToString(reader["node_status"]) ?? "NORMAL",
                IsReferenced = Convert.ToInt32(reader["is_referenced"]) == 1
            });
        }

        return rules;
    }

    public async Task<bool> ExistsAsync(
        int serviceTypeId,
        int nodeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM service_node_rules
            WHERE service_type_id = :serviceTypeId
              AND node_id = :nodeId
            """;
        command.Parameters.Add(new OracleParameter("serviceTypeId", serviceTypeId));
        command.Parameters.Add(new OracleParameter("nodeId", nodeId));

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<bool> InsertAsync(
        int serviceTypeId,
        int nodeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO service_node_rules (service_type_id, node_id)
            SELECT st.service_type_id, n.node_id
            FROM service_types st
            CROSS JOIN nodes n
            WHERE st.service_type_id = :serviceTypeId
              AND st.type_status = 'ENABLED'
              AND n.node_id = :nodeId
              AND n.node_status = 'NORMAL'
              AND NOT EXISTS (
                  SELECT 1
                  FROM service_node_rules existing_rule
                  WHERE existing_rule.service_type_id = st.service_type_id
                    AND existing_rule.node_id = n.node_id
              )
            """;
        command.Parameters.Add(new OracleParameter("serviceTypeId", serviceTypeId));
        command.Parameters.Add(new OracleParameter("nodeId", nodeId));

        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (OracleException exception) when (exception.Number == 1)
        {
            return false;
        }
    }

    public async Task<ServiceNodeRuleRemoveResult> RemoveAsync(
        int serviceTypeId,
        int nodeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM service_node_rules r
            WHERE r.service_type_id = :serviceTypeId
              AND r.node_id = :nodeId
              AND NOT EXISTS (
                  SELECT 1
                  FROM tasks t
                  WHERE t.service_type_id = r.service_type_id
                    AND t.node_id = r.node_id
              )
            """;
        command.Parameters.Add(new OracleParameter("serviceTypeId", serviceTypeId));
        command.Parameters.Add(new OracleParameter("nodeId", nodeId));

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
        {
            return ServiceNodeRuleRemoveResult.Success;
        }

        return await ExistsAsync(serviceTypeId, nodeId, cancellationToken)
            ? ServiceNodeRuleRemoveResult.Referenced
            : ServiceNodeRuleRemoveResult.NotFound;
    }
}

public enum ServiceNodeRuleRemoveResult
{
    Success,
    NotFound,
    Referenced
}
