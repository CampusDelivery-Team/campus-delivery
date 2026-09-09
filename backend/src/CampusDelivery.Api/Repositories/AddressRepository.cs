using System.Data;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CampusDelivery.Api.Repositories;

public sealed class AddressRepository(OracleConnectionFactory connectionFactory) : IAddressRepository
{
    public List<UserAddress> GetAddressesByUserId(int userId)
    {
        var list = new List<UserAddress>();
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT user_id, address_no, contact_name, contact_phone, campus, building_room, is_default
              FROM APPUSER.user_addresses
             WHERE user_id = :userId
             ORDER BY is_default DESC, address_no ASC
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));

        using OracleDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapAddress(reader));
        }

        return list;
    }

    public UserAddress? GetAddress(int userId, int addressNo)
    {
        using OracleConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT user_id, address_no, contact_name, contact_phone, campus, building_room, is_default
              FROM APPUSER.user_addresses
             WHERE user_id = :userId
               AND address_no = :addressNo
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));
        command.Parameters.Add(new OracleParameter("addressNo", addressNo));

        using OracleDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapAddress(reader) : null;
    }

    public async Task<bool> LockUserAsync(
        int userId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "SELECT user_id FROM APPUSER.users WHERE user_id = :userId FOR UPDATE";
        command.Parameters.Add(new OracleParameter("userId", userId));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken);
    }

    public async Task<int> GetNextAddressNoAsync(
        int userId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "SELECT NVL(MAX(address_no), 0) + 1 FROM APPUSER.user_addresses WHERE user_id = :userId";
        command.Parameters.Add(new OracleParameter("userId", userId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> InsertAddressAsync(
        UserAddress address,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.user_addresses
                (user_id, address_no, contact_name, contact_phone, campus, building_room, is_default)
            VALUES
                (:userId, :addressNo, :contactName, :contactPhone, :campus, :buildingRoom, :isDefault)
            """;
        AddAddressParameters(command, address);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<UserAddress?> GetAddressWithLockAsync(
        int userId,
        int addressNo,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT user_id, address_no, contact_name, contact_phone, campus, building_room, is_default
              FROM APPUSER.user_addresses
             WHERE user_id = :userId
               AND address_no = :addressNo
             FOR UPDATE
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));
        command.Parameters.Add(new OracleParameter("addressNo", addressNo));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapAddress(reader) : null;
    }

    public async Task<bool> UpdateAddressAsync(
        UserAddress address,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.user_addresses
               SET contact_name = :contactName,
                   contact_phone = :contactPhone,
                   campus = :campus,
                   building_room = :buildingRoom
             WHERE user_id = :userId
               AND address_no = :addressNo
            """;
        command.Parameters.Add(new OracleParameter("contactName", address.ContactName));
        command.Parameters.Add(new OracleParameter("contactPhone", address.ContactPhone));
        command.Parameters.Add(new OracleParameter("campus", address.Campus));
        command.Parameters.Add(new OracleParameter("buildingRoom", address.BuildingRoom));
        command.Parameters.Add(new OracleParameter("userId", address.UserId));
        command.Parameters.Add(new OracleParameter("addressNo", address.AddressNo));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<DefaultAddressProcedureResult> SetDefaultAddressAsync(
        int userId,
        int addressNo,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "APPUSER.sp_set_default_address";
        command.Parameters.Add(new OracleParameter("p_user_id", userId));
        command.Parameters.Add(new OracleParameter("p_address_no", addressNo));
        var resultParameter = new OracleParameter("p_result", OracleDbType.Varchar2, 40)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(resultParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);

        string result = resultParameter.Value is OracleString oracleString
            ? oracleString.Value
            : Convert.ToString(resultParameter.Value) ?? string.Empty;
        return result.Trim() switch
        {
            "SUCCESS" => DefaultAddressProcedureResult.Success,
            "ALREADY_DEFAULT" => DefaultAddressProcedureResult.AlreadyDefault,
            "USER_NOT_FOUND" => DefaultAddressProcedureResult.UserNotFound,
            "ADDRESS_NOT_FOUND" => DefaultAddressProcedureResult.AddressNotFound,
            _ => DefaultAddressProcedureResult.Failed
        };
    }

    public async Task<bool> DeleteAddressAsync(
        int userId,
        int addressNo,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "DELETE FROM APPUSER.user_addresses WHERE user_id = :userId AND address_no = :addressNo";
        command.Parameters.Add(new OracleParameter("userId", userId));
        command.Parameters.Add(new OracleParameter("addressNo", addressNo));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> SetFirstAddressAsDefaultAsync(
        int userId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.user_addresses
               SET is_default = 'Y'
             WHERE user_id = :userId
               AND address_no = (
                   SELECT MIN(address_no)
                     FROM APPUSER.user_addresses
                    WHERE user_id = :userId
               )
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static void AddAddressParameters(OracleCommand command, UserAddress address)
    {
        command.Parameters.Add(new OracleParameter("userId", address.UserId));
        command.Parameters.Add(new OracleParameter("addressNo", address.AddressNo));
        command.Parameters.Add(new OracleParameter("contactName", address.ContactName));
        command.Parameters.Add(new OracleParameter("contactPhone", address.ContactPhone));
        command.Parameters.Add(new OracleParameter("campus", address.Campus));
        command.Parameters.Add(new OracleParameter("buildingRoom", address.BuildingRoom));
        command.Parameters.Add(new OracleParameter("isDefault", address.IsDefault));
    }

    private static UserAddress MapAddress(OracleDataReader reader) => new()
    {
        UserId = Convert.ToInt32(reader["user_id"]),
        AddressNo = Convert.ToInt32(reader["address_no"]),
        ContactName = Convert.ToString(reader["contact_name"]) ?? string.Empty,
        ContactPhone = Convert.ToString(reader["contact_phone"]) ?? string.Empty,
        Campus = Convert.ToString(reader["campus"]) ?? string.Empty,
        BuildingRoom = Convert.ToString(reader["building_room"]) ?? string.Empty,
        IsDefault = Convert.ToString(reader["is_default"]) ?? "N"
    };
}
