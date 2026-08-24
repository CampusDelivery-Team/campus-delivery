using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class DatabaseRepository(OracleConnectionFactory connectionFactory) : IDatabaseRepository
{
    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.users";
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }
}
