using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class NodeRepository(OracleConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var nodes = new List<Node>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT node_id, node_type, node_name, location, open_time, node_status
            FROM nodes
            ORDER BY node_id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            nodes.Add(MapNode(reader));
        }

        return nodes;
    }

    public async Task<Node?> GetByIdAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT node_id, node_type, node_name, location, open_time, node_status
            FROM nodes
            WHERE node_id = :nodeId
            """;
        command.Parameters.Add(new OracleParameter("nodeId", nodeId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapNode(reader) : null;
    }

    public async Task<bool> ExistsByNameAsync(
        string nodeName,
        int? excludedNodeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = excludedNodeId.HasValue
            ? """
                SELECT COUNT(*)
                FROM nodes
                WHERE UPPER(node_name) = UPPER(:nodeName)
                  AND node_id <> :excludedNodeId
                """
            : """
                SELECT COUNT(*)
                FROM nodes
                WHERE UPPER(node_name) = UPPER(:nodeName)
                """;
        command.Parameters.Add(new OracleParameter("nodeName", nodeName));
        if (excludedNodeId.HasValue)
        {
            command.Parameters.Add(new OracleParameter("excludedNodeId", excludedNodeId.Value));
        }

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task InsertAsync(Node node, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO nodes (node_type, node_name, location, open_time, node_status)
            VALUES (:nodeType, :nodeName, :location, :openTime, :nodeStatus)
            """;
        AddEditableParameters(command, node);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Node node, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE nodes
            SET node_type = :nodeType,
                node_name = :nodeName,
                location = :location,
                open_time = :openTime,
                node_status = :nodeStatus
            WHERE node_id = :nodeId
            """;
        AddEditableParameters(command, node);
        command.Parameters.Add(new OracleParameter("nodeId", node.NodeId));

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateStatusAsync(
        int nodeId,
        string nodeStatus,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE nodes
            SET node_status = :nodeStatus
            WHERE node_id = :nodeId
              AND node_status <> :nodeStatus
            """;
        command.Parameters.Add(new OracleParameter("nodeStatus", nodeStatus));
        command.Parameters.Add(new OracleParameter("nodeId", nodeId));

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<NodeDeleteResult> DeleteAsync(
        int nodeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM nodes n
            WHERE n.node_id = :nodeId
              AND NOT EXISTS (
                  SELECT 1
                  FROM tasks t
                  WHERE t.node_id = n.node_id
              )
            """;
        command.Parameters.Add(new OracleParameter("nodeId", nodeId));

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
        {
            return NodeDeleteResult.Success;
        }

        return await ExistsByIdAsync(nodeId, cancellationToken)
            ? NodeDeleteResult.Referenced
            : NodeDeleteResult.NotFound;
    }

    private async Task<bool> ExistsByIdAsync(
        int nodeId,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM nodes WHERE node_id = :nodeId";
        command.Parameters.Add(new OracleParameter("nodeId", nodeId));

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static Node MapNode(OracleDataReader reader)
    {
        return new Node
        {
            NodeId = Convert.ToInt32(reader["node_id"]),
            NodeType = Convert.ToString(reader["node_type"]) ?? string.Empty,
            NodeName = Convert.ToString(reader["node_name"]) ?? string.Empty,
            Location = Convert.ToString(reader["location"]) ?? string.Empty,
            OpenTime = reader["open_time"] == DBNull.Value ? null : Convert.ToString(reader["open_time"]),
            NodeStatus = Convert.ToString(reader["node_status"]) ?? "NORMAL"
        };
    }

    private static void AddEditableParameters(OracleCommand command, Node node)
    {
        command.Parameters.Add(new OracleParameter("nodeType", node.NodeType));
        command.Parameters.Add(new OracleParameter("nodeName", node.NodeName));
        command.Parameters.Add(new OracleParameter("location", node.Location));
        command.Parameters.Add(new OracleParameter("openTime", (object?)node.OpenTime ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("nodeStatus", node.NodeStatus));
    }
}

public enum NodeDeleteResult
{
    Success,
    NotFound,
    Referenced
}
