using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CampusDelivery.Api.Services;

public sealed class AssignService(
    TaskRepository taskRepository,
    OracleConnectionFactory connectionFactory)
{
    public async Task<bool> GrabTaskAsync(int taskId, int userId, CancellationToken cancellationToken = default)
    {
        var runner = await taskRepository.GetRunnerByUserIdAsync(userId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED" || runner.WorkStatus != "FREE")
        {
            return false;
        }

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (OracleTransaction)(await connection.BeginTransactionAsync(cancellationToken));

        try
        {
            var currentStatus = await taskRepository.GetTaskStatusWithLockAsync(taskId, connection, transaction, cancellationToken);
            if (currentStatus != "WAITING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var lockedRunner = await taskRepository.GetRunnerWithLockAsync(runner.RunnerId, connection, transaction, cancellationToken);
            if (lockedRunner == null || lockedRunner.WorkStatus != "FREE")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await taskRepository.UpdateTaskStatusAsync(taskId, "ASSIGNED", connection, transaction, cancellationToken);
            await taskRepository.UpdateRunnerWorkStatusAsync(runner.RunnerId, "BUSY", connection, transaction, cancellationToken);

            var record = new AssignRecord
            {
                TaskId = taskId,
                RunnerId = runner.RunnerId,
                OperationType = "SELF"
            };
            int recordId = await taskRepository.InsertAssignRecordAsync(record, connection, transaction, cancellationToken);

            var log = new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = "WAITING",
                StatusAfter = "ASSIGNED",
                OperatorUserId = userId
            };
            await taskRepository.InsertTaskStatusLogAsync(log, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> AssignTaskAsync(int taskId, int runnerId, int adminUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (OracleTransaction)(await connection.BeginTransactionAsync(cancellationToken));

        try
        {
            var currentStatus = await taskRepository.GetTaskStatusWithLockAsync(taskId, connection, transaction, cancellationToken);
            if (currentStatus != "WAITING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var runner = await taskRepository.GetRunnerWithLockAsync(runnerId, connection, transaction, cancellationToken);
            if (runner == null || runner.AuditStatus != "APPROVED" || runner.WorkStatus != "FREE")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await taskRepository.UpdateTaskStatusAsync(taskId, "ASSIGNED", connection, transaction, cancellationToken);
            await taskRepository.UpdateRunnerWorkStatusAsync(runnerId, "BUSY", connection, transaction, cancellationToken);

            var record = new AssignRecord
            {
                TaskId = taskId,
                RunnerId = runnerId,
                OperationType = "ADMIN"
            };
            int recordId = await taskRepository.InsertAssignRecordAsync(record, connection, transaction, cancellationToken);

            var log = new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = "WAITING",
                StatusAfter = "ASSIGNED",
                OperatorUserId = adminUserId
            };
            await taskRepository.InsertTaskStatusLogAsync(log, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> ReassignTaskAsync(int taskId, int newRunnerId, string reason, int adminUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (OracleTransaction)(await connection.BeginTransactionAsync(cancellationToken));

        try
        {
            var currentStatus = await taskRepository.GetTaskStatusWithLockAsync(taskId, connection, transaction, cancellationToken);
            if (currentStatus != "ASSIGNED" && currentStatus != "PICKED_UP" && currentStatus != "DELIVERING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var previousAssign = await taskRepository.GetLatestAssignRecordWithConnectionAsync(taskId, connection, transaction, cancellationToken);
            if (previousAssign == null || previousAssign.RunnerId == newRunnerId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var newRunner = await taskRepository.GetRunnerWithLockAsync(newRunnerId, connection, transaction, cancellationToken);
            if (newRunner == null || newRunner.AuditStatus != "APPROVED" || newRunner.WorkStatus != "FREE")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await taskRepository.UpdateRunnerWorkStatusAsync(previousAssign.RunnerId, "FREE", connection, transaction, cancellationToken);
            await taskRepository.UpdateTaskStatusAsync(taskId, "ASSIGNED", connection, transaction, cancellationToken);
            await taskRepository.UpdateRunnerWorkStatusAsync(newRunnerId, "BUSY", connection, transaction, cancellationToken);

            var record = new AssignRecord
            {
                TaskId = taskId,
                RunnerId = newRunnerId,
                OperationType = "REASSIGN",
                ReassignReason = reason
            };
            int recordId = await taskRepository.InsertAssignRecordAsync(record, connection, transaction, cancellationToken);

            var log = new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = currentStatus,
                StatusAfter = "ASSIGNED",
                OperatorUserId = adminUserId
            };
            await taskRepository.InsertTaskStatusLogAsync(log, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int taskId, string targetStatus, int operatorUserId, CancellationToken cancellationToken = default)
    {
        var runner = await taskRepository.GetRunnerByUserIdAsync(operatorUserId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED")
        {
            return false;
        }

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (OracleTransaction)(await connection.BeginTransactionAsync(cancellationToken));

        try
        {
            var currentStatus = await taskRepository.GetTaskStatusWithLockAsync(taskId, connection, transaction, cancellationToken);
            var assignRecord = await taskRepository.GetLatestAssignRecordWithConnectionAsync(taskId, connection, transaction, cancellationToken);
            if (assignRecord == null || assignRecord.RunnerId != runner.RunnerId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            string? expectedStatus = currentStatus switch
            {
                "ASSIGNED" => "PICKED_UP",
                "PICKED_UP" => "DELIVERING",
                "DELIVERING" => "WAIT_CONFIRM",
                _ => null
            };

            if (targetStatus != expectedStatus)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await taskRepository.UpdateTaskStatusAsync(taskId, targetStatus, connection, transaction, cancellationToken);

            var log = new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = currentStatus,
                StatusAfter = targetStatus,
                OperatorUserId = operatorUserId
            };
            await taskRepository.InsertTaskStatusLogAsync(log, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> ConfirmReceiptAsync(int taskId, int publisherUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (OracleTransaction)(await connection.BeginTransactionAsync(cancellationToken));

        try
        {
            var currentStatus = await taskRepository.GetTaskStatusWithLockAsync(taskId, connection, transaction, cancellationToken);
            var taskPublisherUserId = await taskRepository.GetTaskPublisherUserIdAsync(taskId, connection, transaction, cancellationToken);
            var assignRecord = await taskRepository.GetLatestAssignRecordWithConnectionAsync(taskId, connection, transaction, cancellationToken);

            if (currentStatus != "WAIT_CONFIRM" || taskPublisherUserId != publisherUserId || assignRecord == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            bool alreadyConfirmed = await taskRepository.IsReceiptConfirmedAsync(
                assignRecord.RecordId,
                publisherUserId,
                connection,
                transaction,
                cancellationToken);
            if (alreadyConfirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var log = new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = "WAIT_CONFIRM",
                StatusAfter = "WAIT_CONFIRM",
                OperatorUserId = publisherUserId
            };
            await taskRepository.InsertTaskStatusLogAsync(log, connection, transaction, cancellationToken);
            await taskRepository.UpdateRunnerWorkStatusAsync(assignRecord.RunnerId, "FREE", connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

}
