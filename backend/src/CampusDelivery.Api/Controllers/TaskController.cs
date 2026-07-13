using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class TaskController(
    TaskRepository taskRepository,
    AssignService assignService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int size = 5, CancellationToken cancellationToken = default)
    {
        int offset = (page - 1) * size;
        var grabableTasks = await taskRepository.GetGrabableTasksAsync(offset, size, cancellationToken);
        var totalCount = await taskRepository.GetGrabableCountAsync(cancellationToken);

        var runner = await taskRepository.GetRunnerByUserIdAsync(CurrentUserId, cancellationToken);

        var viewModel = new TaskHallViewModel
        {
            PageNumber = page,
            PageSize = size,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / size),
            ActiveRunnerId = runner?.AuditStatus == "APPROVED" ? runner.RunnerId : null,
            ActiveRunnerStatus = runner?.WorkStatus ?? "",
            Tasks = new List<TaskHallItemViewModel>()
        };

        foreach (var task in grabableTasks)
        {
            var (_, _, addressDisplay) = await taskRepository.GetAddressDetailsAsync(task.PublisherUserId, task.AddressNo, cancellationToken);
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

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grab(int taskId, CancellationToken cancellationToken)
    {
        var runner = await taskRepository.GetRunnerByUserIdAsync(CurrentUserId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED")
        {
            TempData["ErrorMessage"] = "您尚未获得审核通过的跑腿员资质，无法接单。";
            return RedirectToAction(nameof(Index));
        }

        if (runner.WorkStatus != "FREE")
        {
            TempData["ErrorMessage"] = "您当前处于繁忙或离线状态，无法接新单。";
            return RedirectToAction(nameof(Index));
        }

        bool success = await assignService.GrabTaskAsync(taskId, CurrentUserId, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "抢单成功！已为您分配该配送任务。";
            return RedirectToAction(nameof(MyTasks));
        }

        TempData["ErrorMessage"] = "抢单失败：该订单已被抢占或状态发生变更。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> MyTasks(CancellationToken cancellationToken = default)
    {
        var runner = await taskRepository.GetRunnerByUserIdAsync(CurrentUserId, cancellationToken);
        if (runner == null || runner.AuditStatus != "APPROVED")
        {
            TempData["ErrorMessage"] = "您没有审核通过的跑腿员账号，无法查看工作台。";
            return RedirectToAction("Index", "Home");
        }

        var activeTasks = await taskRepository.GetActiveTasksByRunnerIdAsync(runner.RunnerId, cancellationToken);

        var viewModel = new MyTasksViewModel
        {
            RunnerId = runner.RunnerId,
            ActiveTasks = new List<MyTaskItemViewModel>()
        };

        foreach (var task in activeTasks)
        {
            var assign = await taskRepository.GetLatestAssignRecordAsync(task.TaskId, cancellationToken);
            if (assign == null) continue;

            var (contactName, contactPhone, addressDisplay) = await taskRepository.GetAddressDetailsAsync(task.PublisherUserId, task.AddressNo, cancellationToken);
            var logs = await taskRepository.GetStatusLogsByRecordIdAsync(assign.RecordId, cancellationToken);

            var taskItem = new MyTaskItemViewModel
            {
                TaskId = task.TaskId,
                RecordId = assign.RecordId,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                UrgentFlag = task.UrgentFlag,
                TaskStatus = task.TaskStatus,
                TaskStatusDisplayName = DisplayNameService.GetTaskStatusName(task.TaskStatus),
                ServiceTypeName = await taskRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                NodeName = await taskRepository.GetNodeNameAsync(task.NodeId, cancellationToken),
                AddressDisplay = addressDisplay,
                ContactName = contactName,
                ContactPhone = contactPhone,
                AssignedAt = assign.AssignedAt,
                ReceiptConfirmed = logs.Any(l => l.StatusBefore == "WAIT_CONFIRM" && l.StatusAfter == "WAIT_CONFIRM"),
                Logs = logs.Select(l => new TaskStatusLogViewModel

                {
                    StatusBeforeDisplayName = l.StatusBefore != null ? DisplayNameService.GetTaskStatusName(l.StatusBefore) : null,
                    StatusAfterDisplayName = DisplayNameService.GetTaskStatusName(l.StatusAfter),
                    ActionName = l.StatusBefore == "WAIT_CONFIRM" && l.StatusAfter == "WAIT_CONFIRM" ? "用户已确认收货" : null,
                    OperatorName = l.OperatorUserId == CurrentUserId ? "您自己" : "系统/管理员或用户",
                    OperatedAt = l.OperatedAt

                }).ToList()
            };

            viewModel.ActiveTasks.Add(taskItem);
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int taskId, string targetStatus, CancellationToken cancellationToken)
    {
        if (targetStatus != "PICKED_UP" && targetStatus != "DELIVERING" && targetStatus != "WAIT_CONFIRM")
        {
            TempData["ErrorMessage"] = "非法的配送流转状态参数。";
            return RedirectToAction(nameof(MyTasks));
        }

        bool success = await assignService.UpdateStatusAsync(taskId, targetStatus, CurrentUserId, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"订单状态已成功更新为【{DisplayNameService.GetTaskStatusName(targetStatus)}】。";
        }
        else
        {
            TempData["ErrorMessage"] = "状态流转更新失败：当前订单状态不支持此变更。";
        }

        return RedirectToAction(nameof(MyTasks));
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetTasksWaitingForReceiptAsync(CurrentUserId, cancellationToken);
        var viewModel = new ReceiptTasksViewModel();

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

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReceipt(int taskId, CancellationToken cancellationToken)
    {
        bool success = await assignService.ConfirmReceiptAsync(taskId, CurrentUserId, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "确认收货成功，订单可以进入收货后支付。";
        }
        else
        {
            TempData["ErrorMessage"] = "确认收货失败：请检查订单状态或发布者身份。";
        }

        return RedirectToAction(nameof(Receipt));
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]

    public async Task<IActionResult> AdminConsole(CancellationToken cancellationToken = default)
    {
        var waitingTasks = await taskRepository.GetWaitingTasksForAdminAsync(cancellationToken);
        var freeRunners = await taskRepository.GetFreeRunnersForAdminAsync(cancellationToken);

        var viewModel = new AdminAssignViewModel
        {
            WaitingTasks = new List<AdminTaskItemViewModel>(),
            FreeRunners = freeRunners.Select(r => new AdminRunnerItemViewModel
            {
                RunnerId = r.RunnerId,
                RealName = r.RealName,
                CreditScore = r.CreditScore
            }).ToList()
        };

        foreach (var task in waitingTasks)
        {
            var (_, _, addressDisplay) = await taskRepository.GetAddressDetailsAsync(task.PublisherUserId, task.AddressNo, cancellationToken);
            viewModel.WaitingTasks.Add(new AdminTaskItemViewModel
            {
                TaskId = task.TaskId,
                TaskTitle = task.TaskTitle,
                TaskPrice = task.TaskPrice,
                ServiceTypeName = await taskRepository.GetServiceTypeNameAsync(task.ServiceTypeId, cancellationToken),
                CreatedAddress = addressDisplay
            });
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminAssign(int taskId, int runnerId, CancellationToken cancellationToken)
    {
        bool success = await assignService.AssignTaskAsync(taskId, runnerId, CurrentUserId, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "指派成功！订单已成功分配给跑腿员。";
        }
        else
        {
            TempData["ErrorMessage"] = "指派失败：跑腿员可能已被占用，或任务状态已变更。";
        }
        return RedirectToAction(nameof(AdminConsole));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminReassign(int taskId, int runnerId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = "管理员后台调度异常重派";
        }

        bool success = await assignService.ReassignTaskAsync(taskId, runnerId, reason, CurrentUserId, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "重派操作成功！订单已重新分派。";
        }
        else
        {
            TempData["ErrorMessage"] = "重派失败：新分配的跑腿员可能处于占用状态，或订单已被其他人签收。";
        }
        return RedirectToAction(nameof(AdminConsole));
    }

}
