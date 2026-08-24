using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Repositories;

public sealed class RunnerRepository(OracleConnectionFactory connectionFactory) : IRunnerRepository
{
    public async Task<IReadOnlyList<Runner>> GetAllAsync(
        bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var runners = new List<Runner>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = pendingOnly
            ? BuildRunnerQuery("WHERE r.audit_status = 'PENDING'")
            : BuildRunnerQuery(string.Empty);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            runners.Add(MapRunner(reader));
        }

        return runners;
    }

    public async Task<Runner?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = BuildRunnerQuery("WHERE r.user_id = :userId");
        command.Parameters.Add(new OracleParameter("userId", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRunner(reader) : null;
    }

    public async Task<bool> CanApplyAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM users
            WHERE user_id = :userId
              AND account_status = 'NORMAL'
              AND user_role <> 'ADMIN'
            """;
        command.Parameters.Add(new OracleParameter("userId", userId));

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<bool> InsertApplicationAsync(
        int userId,
        string realName,
        string identityInfo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO runners (
                user_id,
                real_name,
                identity_info,
                audit_status,
                work_status,
                credit_score
            )
            SELECT u.user_id,
                   :realName,
                   :identityInfo,
                   'PENDING',
                   'OFFLINE',
                   100
            FROM users u
            WHERE u.user_id = :userId
              AND u.account_status = 'NORMAL'
              AND u.user_role <> 'ADMIN'
              AND NOT EXISTS (
                  SELECT 1
                  FROM runners existing_runner
                  WHERE existing_runner.user_id = u.user_id
              )
            """;
        command.Parameters.Add(new OracleParameter("realName", realName));
        command.Parameters.Add(new OracleParameter("identityInfo", identityInfo));
        command.Parameters.Add(new OracleParameter("userId", userId));

        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (OracleException exception) when (exception.Number == 1)
        {
            return false;
        }
    }

    public async Task<bool> ResubmitApplicationAsync(
        int userId,
        string realName,
        string identityInfo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE runners r
            SET real_name = :realName,
                identity_info = :identityInfo,
                audit_status = 'PENDING',
                work_status = 'OFFLINE'
            WHERE r.user_id = :userId
              AND r.audit_status = 'REJECTED'
              AND EXISTS (
                  SELECT 1
                  FROM users u
                  WHERE u.user_id = r.user_id
                    AND u.account_status = 'NORMAL'
                    AND u.user_role <> 'ADMIN'
              )
            """;
        command.Parameters.Add(new OracleParameter("realName", realName));
        command.Parameters.Add(new OracleParameter("identityInfo", identityInfo));
        command.Parameters.Add(new OracleParameter("userId", userId));

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<RunnerReviewWriteResult> ReviewAsync(
        int runnerId,
        string auditStatus,
        string workStatus,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            int userId;
            await using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = """
                    SELECT user_id, audit_status
                    FROM runners
                    WHERE runner_id = :runnerId
                    FOR UPDATE
                    """;
                selectCommand.Parameters.Add(new OracleParameter("runnerId", runnerId));

                await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return RunnerReviewWriteResult.NotFound;
                }

                if (!string.Equals(
                        Convert.ToString(reader["audit_status"]),
                        "PENDING",
                        StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return RunnerReviewWriteResult.AlreadyReviewed;
                }

                userId = Convert.ToInt32(reader["user_id"]);
            }

            if (auditStatus == "APPROVED")
            {
                await using var userCommand = connection.CreateCommand();
                userCommand.Transaction = transaction;
                userCommand.CommandText = """
                    UPDATE users
                    SET user_role = 'RUNNER'
                    WHERE user_id = :userId
                      AND account_status = 'NORMAL'
                      AND user_role <> 'ADMIN'
                    """;
                userCommand.Parameters.Add(new OracleParameter("userId", userId));
                if (await userCommand.ExecuteNonQueryAsync(cancellationToken) == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return RunnerReviewWriteResult.AccountUnavailable;
                }
            }

            await using var runnerCommand = connection.CreateCommand();
            runnerCommand.Transaction = transaction;
            runnerCommand.CommandText = """
                UPDATE runners
                SET audit_status = :auditStatus,
                    work_status = :workStatus
                WHERE runner_id = :runnerId
                  AND audit_status = 'PENDING'
                """;
            runnerCommand.Parameters.Add(new OracleParameter("auditStatus", auditStatus));
            runnerCommand.Parameters.Add(new OracleParameter("workStatus", workStatus));
            runnerCommand.Parameters.Add(new OracleParameter("runnerId", runnerId));

            if (await runnerCommand.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RunnerReviewWriteResult.AlreadyReviewed;
            }

            await transaction.CommitAsync(cancellationToken);
            return RunnerReviewWriteResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RunnerWorkStatusWriteResult> UpdateWorkStatusAsync(
        int runnerId,
        string workStatus,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE runners
            SET work_status = :workStatus
            WHERE runner_id = :runnerId
              AND audit_status = 'APPROVED'
              AND work_status IN ('FREE', 'OFFLINE')
              AND work_status <> :workStatus
            """;
        command.Parameters.Add(new OracleParameter("workStatus", workStatus));
        command.Parameters.Add(new OracleParameter("runnerId", runnerId));

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
        {
            return RunnerWorkStatusWriteResult.Success;
        }

        await using var statusCommand = connection.CreateCommand();
        statusCommand.CommandText = """
            SELECT audit_status, work_status
            FROM runners
            WHERE runner_id = :runnerId
            """;
        statusCommand.Parameters.Add(new OracleParameter("runnerId", runnerId));

        await using var reader = await statusCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return RunnerWorkStatusWriteResult.NotFound;
        }

        return Convert.ToString(reader["work_status"]) == "BUSY"
            ? RunnerWorkStatusWriteResult.Busy
            : RunnerWorkStatusWriteResult.Unavailable;
    }

    private static string BuildRunnerQuery(string whereClause)
    {
        return $"""
            SELECT r.runner_id,
                   r.user_id,
                   u.username,
                   u.phone,
                   u.user_role,
                   u.account_status,
                   r.real_name,
                   r.identity_info,
                   r.audit_status,
                   r.work_status,
                   r.credit_score
            FROM runners r
            JOIN users u ON u.user_id = r.user_id
            {whereClause}
            ORDER BY r.runner_id
            """;
    }

    private static Runner MapRunner(OracleDataReader reader)
    {
        return new Runner
        {
            RunnerId = Convert.ToInt32(reader["runner_id"]),
            UserId = Convert.ToInt32(reader["user_id"]),
            Username = Convert.ToString(reader["username"]) ?? string.Empty,
            Phone = Convert.ToString(reader["phone"]) ?? string.Empty,
            UserRole = Convert.ToString(reader["user_role"]) ?? "USER",
            AccountStatus = Convert.ToString(reader["account_status"]) ?? "NORMAL",
            RealName = Convert.ToString(reader["real_name"]) ?? string.Empty,
            IdentityInfo = Convert.ToString(reader["identity_info"]) ?? string.Empty,
            AuditStatus = Convert.ToString(reader["audit_status"]) ?? "PENDING",
            WorkStatus = Convert.ToString(reader["work_status"]) ?? "OFFLINE",
            CreditScore = Convert.ToDecimal(reader["credit_score"])
        };
    }
}
