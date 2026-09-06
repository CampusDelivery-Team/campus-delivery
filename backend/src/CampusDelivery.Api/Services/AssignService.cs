using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class AssignService(
    IAssignRepository assignRepository,
    IRepositoryTransactionManager transactionManager) : IAssignService
{
    public async Task<TaskHallViewModel> GetTaskHallAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        int totalCount = await assignRepository.GetGrabableCountAsync(cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;

        var tasks = await assignRepository.GetGrabableTasksAsync(offset, pageSize, cancellationToken);
        var runner = await assignRepository.GetRunnerByUserIdAsync(userId, cancellationToken);
        var viewModel = new TaskHallViewModel
        {
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = GetTotalPages(totalCount, pageSize),
            ActiveRunnerId = runner?.AuditStatus == "APPROVED" ? runner.RunnerId : null,
            ActiveRunnerStatus = runner?.WorkStatus ?? string.Empty,
            ActiveTaskCount = runner?.AuditStatus == "APPROVED"
                ? await assignRepository.GetActiveTaskCountByRunnerIdAsync(runner.RunnerId, cancellationToken)
                : 0
        };

        foreach (var task in tasks)
        {
            var (_, _, addressDisplay) = await assignRepository.GetAddressDetailsAsync(
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
                ServiceTypeName = await assignRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                NodeName = await assignRepository.GetNodeNameAsync(task.NodeId, cancellationToken),
                AddressDisplay = addressDisplay,
                CreatedAt = task.CreatedAt,
                IsPublishedByCurrentUser = task.PublisherUserId == userId
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
        var runner = await assignRepository.GetRunnerByUserIdAsync(userId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED")
        {
            return null;
        }

        (page, pageSize) = NormalizePage(page, pageSize);
        int totalCount = await assignRepository.GetActiveTaskCountByRunnerIdAsync(runner.RunnerId, cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;
        var tasks = await assignRepository.GetActiveTasksByRunnerIdAsync(
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
            var assign = await assignRepository.GetLatestAssignRecordAsync(task.TaskId, cancellationToken);
            if (assign == null)
            {
                continue;
            }

            var (contactName, contactPhone, addressDisplay) = await assignRepository.GetAddressDetailsAsync(
                task.PublisherUserId,
                task.AddressNo,
                cancellationToken);
            var logs = await assignRepository.GetStatusLogsByTaskIdAsync(task.TaskId, cancellationToken);
            var details = await assignRepository.GetActiveTaskDetailsAsync(
                task.TaskId,
                runner.RunnerId,
                cancellationToken);
            bool receiptConfirmed = logs.Any(log => IsReceiptConfirmationLog(log, task.PublisherUserId));

            var detailFields = new List<TaskDetailFieldViewModel>();
            void AddDetail(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    detailFields.Add(new TaskDetailFieldViewModel { Label = label, Value = value });
                }
            }

            if (details?.Task.TaskKind == "FOOD")
            {
                AddDetail("商家名称", details.MerchantName);
                AddDetail("平台订单号", details.PlatformOrderNo);
                AddDetail("取餐备注", details.FoodPickupNote);
            }
            else if (details?.Task.TaskKind == "EXPRESS")
            {
                AddDetail("快递公司", details.ExpressCompany);
                AddDetail("物流单号", details.WaybillNo);
                AddDetail("取件码", details.PickupCode);
                AddDetail("取件备注", details.ExpressPickupNote);
            }
            else if (details?.Task.TaskKind == "PRIVATE")
            {
                AddDetail("物品类别", details.ItemCategory);
                AddDetail("取货地点", details.PickupLocation);
                AddDetail("送达地点", details.DeliveryLocation);
                AddDetail("期望完成时间", details.ExpectedFinishAt?.ToString("yyyy-MM-dd HH:mm"));
                AddDetail("任务描述", details.PrivateDescription);
            }

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
                ServiceTypeName = await assignRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                NodeName = await assignRepository.GetNodeNameAsync(task.NodeId, cancellationToken),
                AddressDisplay = addressDisplay,
                ContactName = contactName,
                ContactPhone = contactPhone,
                AssignedAt = assign.AssignedAt,
                ReceiptConfirmed = receiptConfirmed,
                DetailFields = detailFields,

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
        int totalCount = await assignRepository.GetTasksWaitingForReceiptCountAsync(publisherUserId, cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;
        var tasks = await assignRepository.GetTasksWaitingForReceiptAsync(
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
                ServiceTypeName = await assignRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken)
            });
        }

        return viewModel;
    }

    public async Task<AdminAssignViewModel> GetAdminConsoleAsync(
        int taskPage,
        int runnerPage,
        int pageSize,
        string? reassignKeyword,
        string? reassignStatus,
        int reassignPage,
        CancellationToken cancellationToken = default)
    {
        const int reassignPageSize = 10;
        (taskPage, pageSize) = NormalizePage(taskPage, pageSize);
        runnerPage = Math.Max(1, runnerPage);
        reassignPage = Math.Max(1, reassignPage);
        reassignKeyword = string.IsNullOrWhiteSpace(reassignKeyword) ? null : reassignKeyword.Trim();
        if (reassignStatus is not ("ASSIGNED" or "PICKED_UP" or "DELIVERING"))
        {
            reassignStatus = null;
        }

        int taskTotalCount = await assignRepository.GetWaitingTasksForAdminCountAsync(cancellationToken);
        int runnerTotalCount = await assignRepository.GetAvailableRunnersForAdminCountAsync(cancellationToken);
        int reassignTotalCount = await assignRepository.GetReassignableTasksForAdminCountAsync(
            reassignKeyword,
            reassignStatus,
            cancellationToken);
        taskPage = ClampPage(taskPage, taskTotalCount, pageSize);
        runnerPage = ClampPage(runnerPage, runnerTotalCount, pageSize);
        reassignPage = ClampPage(reassignPage, reassignTotalCount, reassignPageSize);

        var waitingTasks = await assignRepository.GetWaitingTasksForAdminAsync(
            (taskPage - 1) * pageSize,
            pageSize,
            cancellationToken);
        var availableRunners = await assignRepository.GetAvailableRunnersForAdminAsync(
            (runnerPage - 1) * pageSize,
            pageSize,
            cancellationToken);
        var reassignRunners = await assignRepository.GetAvailableRunnersForAdminAsync(
            0,
            runnerTotalCount,
            cancellationToken);
        var reassignableTasks = await assignRepository.GetReassignableTasksForAdminAsync(
            reassignKeyword,
            reassignStatus,
            (reassignPage - 1) * reassignPageSize,
            reassignPageSize,
            cancellationToken);

        var viewModel = new AdminAssignViewModel
        {
            TaskPageNumber = taskPage,
            TaskTotalPages = GetTotalPages(taskTotalCount, pageSize),
            TaskTotalCount = taskTotalCount,
            RunnerPageNumber = runnerPage,
            RunnerTotalPages = GetTotalPages(runnerTotalCount, pageSize),
            RunnerTotalCount = runnerTotalCount,
            ReassignKeyword = reassignKeyword,
            ReassignStatus = reassignStatus,
            ReassignPageNumber = reassignPage,
            ReassignTotalPages = GetTotalPages(reassignTotalCount, reassignPageSize),
            PageSize = pageSize
        };

        foreach (var task in reassignableTasks)
        {
            viewModel.ReassignableTasks.Add(new AdminReassignableTaskViewModel
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                TaskStatusDisplayName = DisplayNameService.GetTaskStatusName(task.TaskStatus),
                ServiceTypeName = task.ServiceTypeName,
                CreatedAt = task.CreatedAt,
                CurrentRunnerId = task.CurrentRunnerId,
                CurrentRunnerName = task.CurrentRunnerName
            });
        }

        foreach (var runner in reassignRunners)
        {
            viewModel.ReassignRunners.Add(new AdminRunnerOptionViewModel
            {
                RunnerId = runner.RunnerId,
                UserId = runner.UserId,
                RealName = runner.RealName,
                ActiveTaskCount = await assignRepository.GetActiveTaskCountByRunnerIdAsync(
                    runner.RunnerId,
                    cancellationToken)
            });
        }

        foreach (var runner in availableRunners)
        {
            viewModel.AvailableRunners.Add(new AdminRunnerItemViewModel
            {
                RunnerId = runner.RunnerId,
                UserId = runner.UserId,
                RealName = runner.RealName,
                CreditScore = runner.CreditScore,
                ActiveTaskCount = await assignRepository.GetActiveTaskCountByRunnerIdAsync(
                    runner.RunnerId,
                    cancellationToken)
            });
        }

        foreach (var task in waitingTasks)
        {
            var (_, _, addressDisplay) = await assignRepository.GetAddressDetailsAsync(
                task.PublisherUserId,
                task.AddressNo,
                cancellationToken);
            viewModel.WaitingTasks.Add(new AdminTaskItemViewModel
            {
                TaskId = task.TaskId,
                PublisherUserId = task.PublisherUserId,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                ServiceTypeName = await assignRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                CreatedAddress = addressDisplay
            });
        }

        return viewModel;
    }

    public async Task<bool> GrabTaskAsync(int taskId, int userId, CancellationToken cancellationToken = default)
    {
        var runner = await assignRepository.GetRunnerByUserIdAsync(userId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED" || runner.WorkStatus == "OFFLINE")
        {
            return false;
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            var currentStatus = await assignRepository.GetTaskStatusWithLockAsync(taskId, transaction, cancellationToken);
            if (currentStatus != "WAITING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var publisherUserId = await assignRepository.GetTaskPublisherUserIdAsync(
                taskId,
                transaction,
                cancellationToken);
            if (publisherUserId == userId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var lockedRunner = await assignRepository.GetRunnerWithLockAsync(runner.RunnerId, transaction, cancellationToken);
            if (lockedRunner == null || lockedRunner.AuditStatus != "APPROVED" || lockedRunner.WorkStatus == "OFFLINE")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await assignRepository.UpdateTaskStatusAsync(taskId, "ASSIGNED", transaction, cancellationToken);
            await assignRepository.UpdateRunnerWorkStatusAsync(runner.RunnerId, "BUSY", transaction, cancellationToken);

            var record = new AssignRecord
            {
                TaskId = taskId,
                RunnerId = runner.RunnerId,
                OperationType = "SELF"
            };
            int recordId = await assignRepository.InsertAssignRecordAsync(record, transaction, cancellationToken);
            await assignRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = "WAITING",
                StatusAfter = "ASSIGNED",
                OperatorUserId = userId
            }, transaction, cancellationToken);

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
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            var currentStatus = await assignRepository.GetTaskStatusWithLockAsync(taskId, transaction, cancellationToken);
            if (currentStatus != "WAITING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var runner = await assignRepository.GetRunnerWithLockAsync(runnerId, transaction, cancellationToken);
            if (runner == null || runner.AuditStatus != "APPROVED" || runner.WorkStatus == "OFFLINE")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var publisherUserId = await assignRepository.GetTaskPublisherUserIdAsync(
                taskId,
                transaction,
                cancellationToken);
            if (publisherUserId == runner.UserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await assignRepository.UpdateTaskStatusAsync(taskId, "ASSIGNED", transaction, cancellationToken);
            await assignRepository.UpdateRunnerWorkStatusAsync(runnerId, "BUSY", transaction, cancellationToken);

            var record = new AssignRecord
            {
                TaskId = taskId,
                RunnerId = runnerId,
                OperationType = "ADMIN"
            };
            int recordId = await assignRepository.InsertAssignRecordAsync(record, transaction, cancellationToken);
            await assignRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = "WAITING",
                StatusAfter = "ASSIGNED",
                OperatorUserId = adminUserId
            }, transaction, cancellationToken);

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

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            var currentStatus = await assignRepository.GetTaskStatusWithLockAsync(taskId, transaction, cancellationToken);
            if (currentStatus != "ASSIGNED" && currentStatus != "PICKED_UP" && currentStatus != "DELIVERING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var previousAssign = await assignRepository.GetLatestAssignRecordWithLockAsync(taskId, transaction, cancellationToken);
            if (previousAssign == null || previousAssign.RunnerId == newRunnerId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var newRunner = await assignRepository.GetRunnerWithLockAsync(newRunnerId, transaction, cancellationToken);
            if (newRunner == null || newRunner.AuditStatus != "APPROVED" || newRunner.WorkStatus == "OFFLINE")
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var publisherUserId = await assignRepository.GetTaskPublisherUserIdAsync(
                taskId,
                transaction,
                cancellationToken);
            if (publisherUserId == newRunner.UserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            int previousRunnerOtherTaskCount = await assignRepository.GetOtherActiveTaskCountByRunnerIdAsync(
                previousAssign.RunnerId,
                taskId,
                transaction,
                cancellationToken);
            if (previousRunnerOtherTaskCount == 0)
            {
                await assignRepository.UpdateRunnerWorkStatusAsync(
                    previousAssign.RunnerId,
                    "FREE",
                    transaction,
                    cancellationToken);
            }

            await assignRepository.UpdateTaskStatusAsync(taskId, "ASSIGNED", transaction, cancellationToken);
            await assignRepository.UpdateRunnerWorkStatusAsync(newRunnerId, "BUSY", transaction, cancellationToken);

            var record = new AssignRecord
            {
                TaskId = taskId,
                RunnerId = newRunnerId,
                OperationType = "REASSIGN",
                ReassignReason = reason
            };
            int recordId = await assignRepository.InsertAssignRecordAsync(record, transaction, cancellationToken);
            await assignRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = recordId,
                StatusBefore = currentStatus,
                StatusAfter = "ASSIGNED",
                OperatorUserId = adminUserId
            }, transaction, cancellationToken);

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

        var runner = await assignRepository.GetRunnerByUserIdAsync(operatorUserId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED")
        {
            return false;
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            var currentStatus = await assignRepository.GetTaskStatusWithLockAsync(taskId, transaction, cancellationToken);
            var assignRecord = await assignRepository.GetLatestAssignRecordWithLockAsync(taskId, transaction, cancellationToken);
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

            await assignRepository.UpdateTaskStatusAsync(taskId, targetStatus, transaction, cancellationToken);
            await assignRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = currentStatus,
                StatusAfter = targetStatus,
                OperatorUserId = operatorUserId
            }, transaction, cancellationToken);

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
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            var currentStatus = await assignRepository.GetTaskStatusWithLockAsync(taskId, transaction, cancellationToken);
            var taskPublisherUserId = await assignRepository.GetTaskPublisherUserIdAsync(taskId, transaction, cancellationToken);
            var assignRecord = await assignRepository.GetLatestAssignRecordWithLockAsync(taskId, transaction, cancellationToken);

            if (currentStatus != "WAIT_CONFIRM" || taskPublisherUserId != publisherUserId || assignRecord == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            bool alreadyConfirmed = await assignRepository.IsReceiptConfirmedAsync(
                assignRecord.RecordId,
                publisherUserId,
                transaction,
                cancellationToken);
            if (alreadyConfirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }
            // 收货确认只写入确认日志；支付成功后再结束任务并释放跑腿员。
            await assignRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = "WAIT_CONFIRM",
                StatusAfter = "WAIT_CONFIRM",
                OperatorUserId = publisherUserId
            }, transaction, cancellationToken);

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
