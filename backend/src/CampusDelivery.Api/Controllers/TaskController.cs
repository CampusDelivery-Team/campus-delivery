using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class TaskController(AssignService assignService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int size = 5, CancellationToken cancellationToken = default)
    {
        var viewModel = await assignService.GetTaskHallAsync(CurrentUserId, page, size, cancellationToken);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grab(int taskId, CancellationToken cancellationToken)
    {
        bool success = await assignService.GrabTaskAsync(taskId, CurrentUserId, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "抢单成功，已为您分配该配送任务。";
            return RedirectToAction(nameof(MyTasks));
        }

        TempData["ErrorMessage"] = "抢单失败，请检查跑腿员资质、工作状态或任务状态。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> MyTasks(int page = 1, int size = 5, CancellationToken cancellationToken = default)
    {
        var viewModel = await assignService.GetMyTasksAsync(CurrentUserId, page, size, cancellationToken);
        if (viewModel == null)
        {
            TempData["ErrorMessage"] = "您没有审核通过的跑腿员账号，无法查看工作台。";
            return RedirectToAction("Index", "Home");
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int taskId, string targetStatus, CancellationToken cancellationToken)
    {
        bool success = await assignService.UpdateStatusAsync(taskId, targetStatus, CurrentUserId, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"订单状态已更新为【{DisplayNameService.GetTaskStatusName(targetStatus)}】。";
        }
        else
        {
            TempData["ErrorMessage"] = "状态更新失败，请检查当前任务状态和操作权限。";
        }

        return RedirectToAction(nameof(MyTasks));
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int page = 1, int size = 5, CancellationToken cancellationToken = default)
    {
        var viewModel = await assignService.GetReceiptTasksAsync(CurrentUserId, page, size, cancellationToken);
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
            TempData["ErrorMessage"] = "确认收货失败，请检查订单状态或发布者身份。";
        }

        return RedirectToAction(nameof(Receipt));
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminConsole(
        int taskPage = 1,
        int runnerPage = 1,
        int size = 5,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await assignService.GetAdminConsoleAsync(taskPage, runnerPage, size, cancellationToken);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminAssign(int taskId, int runnerId, CancellationToken cancellationToken)
    {
        bool success = await assignService.AssignTaskAsync(taskId, runnerId, CurrentUserId, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "指派成功，订单已分配给跑腿员。"
            : "指派失败，请检查跑腿员工作状态或任务状态。";
        return RedirectToAction(nameof(AdminConsole));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminReassign(int taskId, int runnerId, string? reason, CancellationToken cancellationToken)
    {
        bool success = await assignService.ReassignTaskAsync(taskId, runnerId, reason, CurrentUserId, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "重派成功，订单已重新分配。"
            : "重派失败，请检查新跑腿员工作状态、原接派记录或任务状态。";
        return RedirectToAction(nameof(AdminConsole));
    }
}
