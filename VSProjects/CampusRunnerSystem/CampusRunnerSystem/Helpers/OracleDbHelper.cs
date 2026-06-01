using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace CampusRunnerSystem.Helpers;

public class OracleDbHelper
{
    private readonly string _connectionString;

    public OracleDbHelper(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleConnection")
            ?? throw new InvalidOperationException("未在 appsettings.json 中找到 ConnectionStrings:OracleConnection。");
    }

    public DataTable QueryDataTable(string sql, params OracleParameter[] parameters)
    {
        try
        {
            using var connection = new OracleConnection(_connectionString);
            using var command = CreateCommand(connection, sql, parameters);
            connection.Open();

            using var reader = command.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);
            return table;
        }
        catch (OracleException ex)
        {
            throw new InvalidOperationException($"Oracle 查询失败：{ex.Message}", ex);
        }
    }

    public int ExecuteNonQuery(string sql, params OracleParameter[] parameters)
    {
        try
        {
            using var connection = new OracleConnection(_connectionString);
            using var command = CreateCommand(connection, sql, parameters);
            connection.Open();
            return command.ExecuteNonQuery();
        }
        catch (OracleException ex)
        {
            throw new InvalidOperationException($"Oracle 执行失败：{ex.Message}", ex);
        }
    }

    public object? ExecuteScalar(string sql, params OracleParameter[] parameters)
    {
        try
        {
            using var connection = new OracleConnection(_connectionString);
            using var command = CreateCommand(connection, sql, parameters);
            connection.Open();
            return command.ExecuteScalar();
        }
        catch (OracleException ex)
        {
            throw new InvalidOperationException($"Oracle 标量查询失败：{ex.Message}", ex);
        }
    }

    private static OracleCommand CreateCommand(OracleConnection connection, string sql, OracleParameter[] parameters)
    {
        var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = sql;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
