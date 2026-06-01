using CampusRunnerSystem.Helpers;
using CampusRunnerSystem.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace CampusRunnerSystem.Repositories;

public class ServiceTypeRepository : IServiceTypeRepository
{
    private readonly OracleDbHelper _dbHelper;

    public ServiceTypeRepository(OracleDbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<ServiceTypeViewModel> GetAllServiceTypes()
    {
        const string sql = @"
SELECT service_type_id, service_name, base_price, distance_rule, urgent_rule, type_status
FROM service_types
ORDER BY service_type_id";

        var table = _dbHelper.QueryDataTable(sql);
        var serviceTypes = new List<ServiceTypeViewModel>();

        foreach (System.Data.DataRow row in table.Rows)
        {
            serviceTypes.Add(MapServiceType(row));
        }

        return serviceTypes;
    }

    public ServiceTypeViewModel? GetServiceTypeById(int serviceTypeId)
    {
        const string sql = @"
SELECT service_type_id, service_name, base_price, distance_rule, urgent_rule, type_status
FROM service_types
WHERE service_type_id = :service_type_id";

        var table = _dbHelper.QueryDataTable(sql, new OracleParameter("service_type_id", serviceTypeId));
        return table.Rows.Count == 0 ? null : MapServiceType(table.Rows[0]);
    }

    public void AddServiceType(ServiceTypeViewModel serviceType)
    {
        const string sql = @"
INSERT INTO service_types (service_name, base_price, distance_rule, urgent_rule, type_status)
VALUES (:service_name, :base_price, :distance_rule, :urgent_rule, :type_status)";

        _dbHelper.ExecuteNonQuery(
            sql,
            new OracleParameter("service_name", serviceType.ServiceName),
            new OracleParameter("base_price", serviceType.BasePrice),
            new OracleParameter("distance_rule", string.IsNullOrWhiteSpace(serviceType.DistanceRule) ? DBNull.Value : serviceType.DistanceRule),
            new OracleParameter("urgent_rule", string.IsNullOrWhiteSpace(serviceType.UrgentRule) ? DBNull.Value : serviceType.UrgentRule),
            new OracleParameter("type_status", serviceType.TypeStatus));
    }

    public void UpdateServiceType(ServiceTypeViewModel serviceType)
    {
        const string sql = @"
UPDATE service_types
SET service_name = :service_name,
    base_price = :base_price,
    distance_rule = :distance_rule,
    urgent_rule = :urgent_rule,
    type_status = :type_status
WHERE service_type_id = :service_type_id";

        _dbHelper.ExecuteNonQuery(
            sql,
            new OracleParameter("service_name", serviceType.ServiceName),
            new OracleParameter("base_price", serviceType.BasePrice),
            new OracleParameter("distance_rule", string.IsNullOrWhiteSpace(serviceType.DistanceRule) ? DBNull.Value : serviceType.DistanceRule),
            new OracleParameter("urgent_rule", string.IsNullOrWhiteSpace(serviceType.UrgentRule) ? DBNull.Value : serviceType.UrgentRule),
            new OracleParameter("type_status", serviceType.TypeStatus),
            new OracleParameter("service_type_id", serviceType.ServiceTypeId));
    }

    public void DeleteServiceType(int serviceTypeId)
    {
        const string sql = "DELETE FROM service_types WHERE service_type_id = :service_type_id";
        _dbHelper.ExecuteNonQuery(sql, new OracleParameter("service_type_id", serviceTypeId));
    }

    public int CountTaskReferences(int serviceTypeId)
    {
        const string sql = "SELECT COUNT(*) FROM tasks WHERE service_type_id = :service_type_id";
        var value = _dbHelper.ExecuteScalar(sql, new OracleParameter("service_type_id", serviceTypeId));
        return Convert.ToInt32(value);
    }

    public int CountRuleReferences(int serviceTypeId)
    {
        const string sql = "SELECT COUNT(*) FROM service_node_rules WHERE service_type_id = :service_type_id";
        var value = _dbHelper.ExecuteScalar(sql, new OracleParameter("service_type_id", serviceTypeId));
        return Convert.ToInt32(value);
    }

    private static ServiceTypeViewModel MapServiceType(System.Data.DataRow row)
    {
        return new ServiceTypeViewModel
        {
            ServiceTypeId = Convert.ToInt32(row["service_type_id"]),
            ServiceName = row["service_name"].ToString() ?? string.Empty,
            BasePrice = Convert.ToDecimal(row["base_price"]),
            DistanceRule = row["distance_rule"].ToString() ?? string.Empty,
            UrgentRule = row["urgent_rule"].ToString() ?? string.Empty,
            TypeStatus = row["type_status"].ToString() ?? string.Empty
        };
    }
}
