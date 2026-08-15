using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CampusDelivery.Api.Repositories;

public sealed class ComplaintRepository(OracleConnectionFactory connectionFactory) : IComplaintRepository
{
    private const string Projection = "SELECT complaint_id, record_id, reason, process_status, process_result FROM APPUSER.complaints ";

    public async Task<Complaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = Projection + " WHERE complaint_id = :id";
        command.Parameters.Add(new OracleParameter("id", complaintId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapComplaint(reader) : null;
    }

    public async Task<Complaint?> GetByIdWithLockAsync(int complaintId, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = Projection + " WHERE complaint_id = :id FOR UPDATE";
        command.Parameters.Add(new OracleParameter("id", complaintId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapComplaint(reader) : null;
    }

    public async Task<Complaint?> GetByRecordIdAsync(int recordId, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = Projection + " WHERE record_id = :recordId";
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapComplaint(reader) : null;
    }

    public async Task<ComplaintContext?> GetContextByRecordIdAsync(int recordId, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT ar.task_id, ar.runner_id, t.publisher_user_id, t.task_status, t.task_title
              FROM APPUSER.assign_records ar
              JOIN APPUSER.tasks t ON ar.task_id = t.task_id
             WHERE ar.record_id = :recordId
             FOR UPDATE OF ar.runner_id
            """;
        command.Parameters.Add(new OracleParameter("recordId", recordId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ComplaintContext(
            Convert.ToInt32(reader["task_id"]),
            Convert.ToInt32(reader["runner_id"]),
            Convert.ToInt32(reader["publisher_user_id"]),
            Convert.ToString(reader["task_status"]) ?? string.Empty,
            Convert.ToString(reader["task_title"]) ?? string.Empty);
    }

    public async Task<IReadOnlyList<Complaint>> GetAllPagedAsync(int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var list = new List<Complaint>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = Projection + " ORDER BY complaint_id DESC OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY";
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

    public async Task<IReadOnlyList<Complaint>> GetByUserIdAsync(int userId, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var list = new List<Complaint>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = "SELECT c.complaint_id, c.record_id, c.reason, c.process_status, c.process_result FROM APPUSER.complaints c JOIN APPUSER.assign_records ar ON c.record_id = ar.record_id JOIN APPUSER.tasks t ON ar.task_id = t.task_id WHERE t.publisher_user_id = :userId ORDER BY c.complaint_id DESC OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY";
        command.Parameters.Add(new OracleParameter("userId", userId));
        command.Parameters.Add(new OracleParameter("offset", offset));
        command.Parameters.Add(new OracleParameter("pageSize", pageSize));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapComplaint(reader));
        }
        return list;
    }

    public async Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = "SELECT COUNT(*) FROM APPUSER.complaints c JOIN APPUSER.assign_records ar ON c.record_id = ar.record_id JOIN APPUSER.tasks t ON ar.task_id = t.task_id WHERE t.publisher_user_id = :userId";
        command.Parameters.Add(new OracleParameter("userId", userId));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> InsertAsync(Complaint complaint, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "INSERT INTO APPUSER.complaints (record_id, reason, process_status, process_result) VALUES (:recordId, :reason, :processStatus, :processResult)";
        command.Parameters.Add(new OracleParameter("recordId", complaint.RecordId));
        command.Parameters.Add(new OracleParameter("reason", complaint.Reason));
        command.Parameters.Add(new OracleParameter("processStatus", complaint.ProcessStatus));
        command.Parameters.Add(new OracleParameter("processResult", (object?)complaint.ProcessResult ?? DBNull.Value));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateAsync(Complaint complaint, IRepositoryTransaction repositoryTransaction, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = "UPDATE APPUSER.complaints SET process_status = :processStatus, process_result = :processResult WHERE complaint_id = :complaintId";
        command.Parameters.Add(new OracleParameter("processStatus", complaint.ProcessStatus));
        command.Parameters.Add(new OracleParameter("processResult", (object?)complaint.ProcessResult ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("complaintId", complaint.ComplaintId));
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateRunnerCreditAsync(
        int runnerId,
        decimal creditDelta,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = repositoryTransaction.GetOracle();
        await using OracleCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE APPUSER.runners
               SET credit_score = GREATEST(0, credit_score + :creditDelta)
             WHERE runner_id = :runnerId
            """;
        command.Parameters.Add(new OracleParameter("creditDelta", creditDelta));
        command.Parameters.Add(new OracleParameter("runnerId", runnerId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
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
