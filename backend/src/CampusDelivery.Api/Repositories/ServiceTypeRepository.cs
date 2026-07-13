using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class ServiceTypeRepository(OracleConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<ServiceType>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var serviceTypes = new List<ServiceType>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT service_type_id,
                   service_name,
                   base_price,
                   distance_rule,
                   urgent_rule,
                   type_status
            FROM service_types
            ORDER BY service_type_id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            serviceTypes.Add(MapServiceType(reader));
        }

        return serviceTypes;
    }

    public async Task<bool> ExistsByNameAsync(
        string serviceName,
        int? excludedServiceTypeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = excludedServiceTypeId.HasValue
            ? """
                SELECT COUNT(*)
                FROM service_types
                WHERE UPPER(service_name) = UPPER(:serviceName)
                  AND service_type_id <> :excludedServiceTypeId
                """
            : """
                SELECT COUNT(*)
                FROM service_types
                WHERE UPPER(service_name) = UPPER(:serviceName)
                """;
        command.Parameters.Add(new OracleParameter("serviceName", serviceName));
        if (excludedServiceTypeId.HasValue)
        {
            command.Parameters.Add(new OracleParameter(
                "excludedServiceTypeId",
                excludedServiceTypeId.Value));
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task InsertAsync(
        ServiceType serviceType,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO service_types (
                service_name,
                base_price,
                distance_rule,
                urgent_rule,
                type_status
            )
            VALUES (
                :serviceName,
                :basePrice,
                :distanceRule,
                :urgentRule,
                :typeStatus
            )
            """;
        AddEditableParameters(command, serviceType);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        ServiceType serviceType,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE service_types
            SET service_name = :serviceName,
                base_price = :basePrice,
                distance_rule = :distanceRule,
                urgent_rule = :urgentRule,
                type_status = :typeStatus
            WHERE service_type_id = :serviceTypeId
            """;
        AddEditableParameters(command, serviceType);
        command.Parameters.Add(new OracleParameter("serviceTypeId", serviceType.ServiceTypeId));

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateStatusAsync(
        int serviceTypeId,
        string typeStatus,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE service_types
            SET type_status = :typeStatus
            WHERE service_type_id = :serviceTypeId
              AND type_status <> :typeStatus
            """;
        command.Parameters.Add(new OracleParameter("typeStatus", typeStatus));
        command.Parameters.Add(new OracleParameter("serviceTypeId", serviceTypeId));

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<ServiceTypeDeleteResult> DeleteAsync(
        int serviceTypeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM service_types st
            WHERE st.service_type_id = :serviceTypeId
              AND NOT EXISTS (
                  SELECT 1
                  FROM tasks t
                  WHERE t.service_type_id = st.service_type_id
              )
            """;
        command.Parameters.Add(new OracleParameter("serviceTypeId", serviceTypeId));

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
        {
            return ServiceTypeDeleteResult.Success;
        }

        return await ExistsByIdAsync(serviceTypeId, cancellationToken)
            ? ServiceTypeDeleteResult.Referenced
            : ServiceTypeDeleteResult.NotFound;
    }

    private async Task<bool> ExistsByIdAsync(
        int serviceTypeId,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM service_types WHERE service_type_id = :serviceTypeId";
        command.Parameters.Add(new OracleParameter("serviceTypeId", serviceTypeId));

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static ServiceType MapServiceType(OracleDataReader reader)
    {
        return new ServiceType
        {
            ServiceTypeId = Convert.ToInt32(reader["service_type_id"]),
            ServiceName = Convert.ToString(reader["service_name"]) ?? string.Empty,
            BasePrice = Convert.ToDecimal(reader["base_price"]),
            DistanceRule = reader["distance_rule"] == DBNull.Value
                ? null
                : Convert.ToString(reader["distance_rule"]),
            UrgentRule = reader["urgent_rule"] == DBNull.Value
                ? null
                : Convert.ToString(reader["urgent_rule"]),
            TypeStatus = Convert.ToString(reader["type_status"]) ?? "ENABLED"
        };
    }

    private static void AddEditableParameters(
        OracleCommand command,
        ServiceType serviceType)
    {
        command.Parameters.Add(new OracleParameter("serviceName", serviceType.ServiceName));
        command.Parameters.Add(new OracleParameter("basePrice", (object)serviceType.BasePrice));
        command.Parameters.Add(new OracleParameter(
            "distanceRule",
            (object?)serviceType.DistanceRule ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter(
            "urgentRule",
            (object?)serviceType.UrgentRule ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("typeStatus", serviceType.TypeStatus));
    }
}

public enum ServiceTypeDeleteResult
{
    Success,
    NotFound,
    Referenced
}
