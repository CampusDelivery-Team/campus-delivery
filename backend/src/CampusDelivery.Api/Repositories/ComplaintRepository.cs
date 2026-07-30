using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class ComplaintRepository(OracleConnectionFactory connectionFactory)
{
    public async Task<Complaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT complaint_id, record_id, reason, process_status, process_result
            FROM APPUSER.complaints WHERE complaint_id = :id
            """;
        command.Parameters.Add(new OracleParameter("id", complaintId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapComplaint(reader) : null;
    }

    public async Task<Complaint?> GetByRecordIdAsync(int recordId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT complaint_id, record_id, reason, process_status, process_result
            FROM APPUSER.complaints WHERE record_id = :recordId
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapComplaint(reader) : null;
    }

    public async Task<IReadOnlyList<Complaint>> GetAllPagedAsync(int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var list = new List<Complaint>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT complaint_id, record_id, reason, process_status, process_result
            FROM APPUSER.complaints
            ORDER BY complaint_id DESC
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapComplaint(reader));
        }
        return list;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.complaints";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> InsertAsync(Complaint complaint, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO APPUSER.complaints (record_id, reason, process_status, process_result)
            VALUES (:recordId, :reason, :processStatus, :processResult)
            """;
        command.Parameters.Add(new OracleParameter("recordId", complaint.RecordId));
        command.Parameters.Add(new OracleParameter("reason", complaint.Reason));
        command.Parameters.Add(new OracleParameter("processStatus", complaint.ProcessStatus));
        command.Parameters.Add(new OracleParameter("processResult", (object?)complaint.ProcessResult ?? DBNull.Value));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateAsync(Complaint complaint, OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            UPDATE  APPUSER.complaints
            SET process_status = :processStatus, process_result = :processResult
            WHERE complaint_id = :complaintId
            """;
        command.Parameters.Add(new OracleParameter("processStatus", complaint.ProcessStatus));
        command.Parameters.Add(new OracleParameter("processResult", (object?)complaint.ProcessResult ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("complaintId", complaint.ComplaintId));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Complaint MapComplaint(OracleDataReader reader)
    {
        return new Complaint
        {
            ComplaintId = Convert.ToInt32(reader["complaint_id"]),
            RecordId = Convert.ToInt32(reader["record_id"]),
            Reason = Convert.ToString(reader["reason"]) ?? string.Empty,
            ProcessStatus = Convert.ToString(reader["process_status"]) ?? "SUBMITTED",
            ProcessResult = reader["process_result"] == DBNull.Value ? null : Convert.ToString(reader["process_result"])
        };
    }
}