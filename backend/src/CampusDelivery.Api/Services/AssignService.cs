using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class AssignService(
    TaskRepository taskRepository,
    OracleConnectionFactory connectionFactory)
{
    public async Task<TaskHallViewModel> GetTaskHallAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        int totalCount = await taskRepository.GetGrabableCountAsync(cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;

        var tasks = await taskRepository.GetGrabableTasksAsync(offset, pageSize, cancellationToken);
        var runner = await taskRepository.GetRunnerByUserIdAsync(userId, cancellationToken);
        var viewModel = new TaskHallViewModel
        {
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = GetTotalPages(totalCount, pageSize),
            ActiveRunnerId = runner?.AuditStatus == "APPROVED" ? runner.RunnerId : null,
            ActiveRunnerStatus = runner?.WorkStatus ?? string.Empty
        };

        foreach (var task in tasks)
        {
            var (_, _, addressDisplay) = await taskRepository.GetAddressDetailsAsync(
                task.PublisherUserId,
                task.AddressNo,
                cancellationToken);
            viewModel.Tasks.Add(new TaskHallItemViewModel
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                UrgentFlag = task.UrgentFlag,
                TaskStatus = task.TaskStatus,
                TaskStatusDisplayName = DisplayNameService.GetTaskStatusName(task.TaskStatus),
                ServiceTypeName = await taskRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                NodeName = await taskRepository.GetNodeNameAsync(task.NodeId, cancellationToken),
                AddressDisplay = addressDisplay,
                CreatedAt = task.CreatedAt
            });
        }

        return viewModel;
    }

    public async Task<MyTasksViewModel?> GetMyTasksAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var runner = await taskRepository.GetRunnerByUserIdAsync(userId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED")
        {
            return null;
        }

        (page, pageSize) = NormalizePage(page, pageSize);
        int totalCount = await taskRepository.GetActiveTaskCountByRunnerIdAsync(runner.RunnerId, cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;
        var tasks = await taskRepository.GetActiveTasksByRunnerIdAsync(
            runner.RunnerId,
            offset,
            pageSize,
            cancellationToken);

        var viewModel = new MyTasksViewModel
        {
            RunnerId = runner.RunnerId,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = GetTotalPages(totalCount, pageSize)
        };

        foreach (var task in tasks)
        {
            var assign = await taskRepository.GetLatestAssignRecordAsync(task.TaskId, cancellationToken);
            if (assign == null)
            {
                continue;
            }

            var (contactName, contactPhone, addressDisplay) = await taskRepository.GetAddressDetailsAsync(
                task.PublisherUserId,
                task.AddressNo,
                cancellationToken);
            var logs = await taskRepository.GetStatusLogsByTaskIdAsync(task.TaskId, cancellationToken);
            bool receiptConfirmed = logs.Any(log => IsReceiptConfirmationLog(log, task.PublisherUserId));

            viewModel.ActiveTasks.Add(new MyTaskItemViewModel
            {
                TaskId = task.TaskId,
                RecordId = assign.RecordId,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                UrgentFlag = task.UrgentFlag,
                TaskStatus = task.TaskStatus,
                TaskStatusDisplayName = task.TaskStatus == "WAIT_CONFIRM" && receiptConfirmed
                    ? "待支付"
                    : DisplayNameService.GetTaskStatusName(task.TaskStatus),
                ServiceTypeName = await taskRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                NodeName = await taskRepository.GetNodeNameAsync(task.NodeId, cancellationToken),
                AddressDisplay = addressDisplay,
                ContactName = contactName,
                ContactPhone = contactPhone,
                AssignedAt = assign.AssignedAt,
                ReceiptConfirmed = receiptConfirmed,

                Logs = logs.Select(log => new TaskStatusLogViewModel
                {
                    StatusBeforeDisplayName = log.StatusBefore == null
                        ? null
                        : DisplayNameService.GetTaskStatusName(log.StatusBefore),
                    StatusAfterDisplayName = DisplayNameService.GetTaskStatusName(log.StatusAfter),
                    ActionName = IsReceiptConfirmationLog(log, task.PublisherUserId) ? "用户已确认收货" : null,
                    OperatorName = log.OperatorUserId == userId ? "您自己" : "管理员或任务相关用户",
                    OperatedAt = log.OperatedAt
                }).ToList()
            });

        }

        return viewModel;
    }

    public async Task<ReceiptTasksViewModel> GetReceiptTasksAsync(
        int publisherUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        int totalCount = await taskRepository.GetTasksWaitingForReceiptCountAsync(publisherUserId, cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;
        var tasks = await taskRepository.GetTasksWaitingForReceiptAsync(
            publisherUserId,
            offset,
            pageSize,
            cancellationToken);

        var viewModel = new ReceiptTasksViewModel
        {
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = GetTotalPages(totalCount, pageSize)
        };

        foreach (var task in tasks)
        {
            viewModel.Tasks.Add(new ReceiptTaskItemViewModel
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                ServiceTypeName = await taskRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken)
            });
        }

        return viewModel;
    }

    public async Task<AdminAssignViewModel> GetAdminConsoleAsync(
        int taskPage,
        int runnerPage,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (taskPage, pageSize) = NormalizePage(taskPage, pageSize);
        runnerPage = Math.Max(1, runnerPage);

        int taskTotalCount = await taskRepository.GetWaitingTasksForAdminCountAsync(cancellationToken);
        int runnerTotalCount = await taskRepository.GetFreeRunnersForAdminCountAsync(cancellationToken);
        taskPage = ClampPage(taskPage, taskTotalCount, pageSize);
        runnerPage = ClampPage(runnerPage, runnerTotalCount, pageSize);

        var waitingTasks = await taskRepository.GetWaitingTasksForAdminAsync(
            (taskPage - 1) * pageSize,
            pageSize,
            cancellationToken);
        var freeRunners = await taskRepository.GetFreeRunnersForAdminAsync(
            (runnerPage - 1) * pageSize,
            pageSize,
            cancellationToken);

        var viewModel = new AdminAssignViewModel
        {
            TaskPageNumber = taskPage,
            TaskTotalPages = GetTotalPages(taskTotalCount, pageSize),
            TaskTotalCount = taskTotalCount,
            RunnerPageNumber = runnerPage,
            RunnerTotalPages = GetTotalPages(runnerTotalCount, pageSize),
            RunnerTotalCount = runnerTotalCount,
            PageSize = pageSize,
            FreeRunners = freeRunners.Select(runner => new AdminRunnerItemViewModel
            {
                RunnerId = runner.RunnerId,
                RealName = runner.RealName,
                CreditScore = runner.CreditScore
            }).ToList()
        };

        foreach (var task in waitingTasks)
        {
            var (_, _, addressDisplay) = await taskRepository.GetAddressDetailsAsync(
                task.PublisherUserId,
                task.AddressNo,
                cancellationToken);
            viewModel.WaitingTasks.Add(new AdminTaskItemViewModel
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                ServiceTypeName = await taskRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                CreatedAddress = addressDisplay
            });
        }

        return viewModel;
    }

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
            await taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = "WAITING",
                StatusAfter = "ASSIGNED",
                OperatorUserId = userId
            }, connection, transaction, cancellationToken);

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
            await taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = "WAITING",
                StatusAfter = "ASSIGNED",
                OperatorUserId = adminUserId
            }, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> ReassignTaskAsync(
        int taskId,
        int newRunnerId,
        string? reason,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? "管理员后台调度异常重派" : reason.Trim();

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
            await taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = currentStatus,
                StatusAfter = "ASSIGNED",
                OperatorUserId = adminUserId
            }, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(
        int taskId,
        string targetStatus,
        int operatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (targetStatus != "PICKED_UP" && targetStatus != "DELIVERING" && targetStatus != "WAIT_CONFIRM")
        {
            return false;
        }

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
            await taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = currentStatus,
                StatusAfter = targetStatus,
                OperatorUserId = operatorUserId
            }, connection, transaction, cancellationToken);

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
            // 收货确认只写入确认日志；支付成功后再结束任务并释放跑腿员。
            await taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = "WAIT_CONFIRM",
                StatusAfter = "WAIT_CONFIRM",
                OperatorUserId = publisherUserId
            }, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 20 ? pageSize : 5;
        return (page, pageSize);
    }

    private static int ClampPage(int page, int totalCount, int pageSize)
    {
        int totalPages = GetTotalPages(totalCount, pageSize);
        return totalPages == 0 ? 1 : Math.Min(page, totalPages);
    }

    private static int GetTotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling((double)totalCount / pageSize);
    }

    private static bool IsReceiptConfirmationLog(TaskStatusLog log, int publisherUserId)
    {
        return log.StatusBefore == "WAIT_CONFIRM"
            && log.StatusAfter == "WAIT_CONFIRM"
            && log.OperatorUserId == publisherUserId;
    }

}
