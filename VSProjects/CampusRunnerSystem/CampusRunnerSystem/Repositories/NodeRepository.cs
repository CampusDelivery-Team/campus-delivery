using CampusRunnerSystem.Helpers;
using CampusRunnerSystem.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace CampusRunnerSystem.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly OracleDbHelper _dbHelper;

    public NodeRepository(OracleDbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<NodeViewModel> GetAllNodes()
    {
        const string sql = @"
SELECT node_id, node_type, node_name, location, open_time, node_status
FROM nodes
ORDER BY node_id";

        var table = _dbHelper.QueryDataTable(sql);
        var nodes = new List<NodeViewModel>();

        foreach (System.Data.DataRow row in table.Rows)
        {
            nodes.Add(MapNode(row));
        }

        return nodes;
    }

    public NodeViewModel? GetNodeById(int nodeId)
    {
        const string sql = @"
SELECT node_id, node_type, node_name, location, open_time, node_status
FROM nodes
WHERE node_id = :node_id";

        var table = _dbHelper.QueryDataTable(sql, new OracleParameter("node_id", nodeId));
        return table.Rows.Count == 0 ? null : MapNode(table.Rows[0]);
    }

    public void AddNode(NodeViewModel node)
    {
        const string sql = @"
INSERT INTO nodes (node_type, node_name, location, open_time, node_status)
VALUES (:node_type, :node_name, :location, :open_time, :node_status)";

        _dbHelper.ExecuteNonQuery(
            sql,
            new OracleParameter("node_type", node.NodeType),
            new OracleParameter("node_name", node.NodeName),
            new OracleParameter("location", node.Location),
            new OracleParameter("open_time", string.IsNullOrWhiteSpace(node.OpenTime) ? DBNull.Value : node.OpenTime),
            new OracleParameter("node_status", node.NodeStatus));
    }

    public void UpdateNode(NodeViewModel node)
    {
        const string sql = @"
UPDATE nodes
SET node_type = :node_type,
    node_name = :node_name,
    location = :location,
    open_time = :open_time,
    node_status = :node_status
WHERE node_id = :node_id";

        _dbHelper.ExecuteNonQuery(
            sql,
            new OracleParameter("node_type", node.NodeType),
            new OracleParameter("node_name", node.NodeName),
            new OracleParameter("location", node.Location),
            new OracleParameter("open_time", string.IsNullOrWhiteSpace(node.OpenTime) ? DBNull.Value : node.OpenTime),
            new OracleParameter("node_status", node.NodeStatus),
            new OracleParameter("node_id", node.NodeId));
    }

    public void DeleteNode(int nodeId)
    {
        const string sql = "DELETE FROM nodes WHERE node_id = :node_id";
        _dbHelper.ExecuteNonQuery(sql, new OracleParameter("node_id", nodeId));
    }

    public int CountTaskReferences(int nodeId)
    {
        const string sql = "SELECT COUNT(*) FROM tasks WHERE node_id = :node_id";
        var value = _dbHelper.ExecuteScalar(sql, new OracleParameter("node_id", nodeId));
        return Convert.ToInt32(value);
    }

    public int CountRuleReferences(int nodeId)
    {
        const string sql = "SELECT COUNT(*) FROM service_node_rules WHERE node_id = :node_id";
        var value = _dbHelper.ExecuteScalar(sql, new OracleParameter("node_id", nodeId));
        return Convert.ToInt32(value);
    }

    private static NodeViewModel MapNode(System.Data.DataRow row)
    {
        return new NodeViewModel
        {
            NodeId = Convert.ToInt32(row["node_id"]),
            NodeType = row["node_type"].ToString() ?? string.Empty,
            NodeName = row["node_name"].ToString() ?? string.Empty,
            Location = row["location"].ToString() ?? string.Empty,
            OpenTime = row["open_time"].ToString() ?? string.Empty,
            NodeStatus = row["node_status"].ToString() ?? string.Empty
        };
    }
}
