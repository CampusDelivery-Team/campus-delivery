using CampusRunnerSystem.Helpers;
using CampusRunnerSystem.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace CampusRunnerSystem.Repositories;

public class ServiceNodeRuleRepository : IServiceNodeRuleRepository
{
    private readonly OracleDbHelper _dbHelper;

    public ServiceNodeRuleRepository(OracleDbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<ServiceNodeRuleViewModel> GetAllRules()
    {
        const string sql = @"
SELECT r.service_type_id,
       r.node_id,
       s.service_name,
       n.node_name,
       n.node_type
FROM service_node_rules r
JOIN service_types s ON r.service_type_id = s.service_type_id
JOIN nodes n ON r.node_id = n.node_id
ORDER BY r.service_type_id, r.node_id";

        var table = _dbHelper.QueryDataTable(sql);
        var rules = new List<ServiceNodeRuleViewModel>();

        foreach (System.Data.DataRow row in table.Rows)
        {
            rules.Add(new ServiceNodeRuleViewModel
            {
                ServiceTypeId = Convert.ToInt32(row["service_type_id"]),
                NodeId = Convert.ToInt32(row["node_id"]),
                ServiceName = row["service_name"].ToString() ?? string.Empty,
                NodeName = row["node_name"].ToString() ?? string.Empty,
                NodeType = row["node_type"].ToString() ?? string.Empty
            });
        }

        return rules;
    }

    public bool Exists(int serviceTypeId, int nodeId)
    {
        const string sql = @"
SELECT COUNT(*)
FROM service_node_rules
WHERE service_type_id = :service_type_id
  AND node_id = :node_id";

        var value = _dbHelper.ExecuteScalar(
            sql,
            new OracleParameter("service_type_id", serviceTypeId),
            new OracleParameter("node_id", nodeId));

        return Convert.ToInt32(value) > 0;
    }

    public void AddRule(int serviceTypeId, int nodeId)
    {
        const string sql = @"
INSERT INTO service_node_rules (service_type_id, node_id)
VALUES (:service_type_id, :node_id)";

        _dbHelper.ExecuteNonQuery(
            sql,
            new OracleParameter("service_type_id", serviceTypeId),
            new OracleParameter("node_id", nodeId));
    }

    public void DeleteRule(int serviceTypeId, int nodeId)
    {
        const string sql = @"
DELETE FROM service_node_rules
WHERE service_type_id = :service_type_id
  AND node_id = :node_id";

        _dbHelper.ExecuteNonQuery(
            sql,
            new OracleParameter("service_type_id", serviceTypeId),
            new OracleParameter("node_id", nodeId));
    }
}
