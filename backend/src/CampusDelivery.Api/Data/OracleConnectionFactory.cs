using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Data;

public sealed class OracleConnectionFactory(IConfiguration configuration)
{
    private const string ConnectionStringName = "OracleDb";

    public OracleConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing.");
        }

        return new OracleConnection(connectionString);
    }
}
