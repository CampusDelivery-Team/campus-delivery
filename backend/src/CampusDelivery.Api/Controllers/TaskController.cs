using System.Security.Claims;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers
{
    [Authorize]
    public sealed class TaskController : Controller
    {
        private readonly TaskService _taskService;
        private readonly AssignService _assignService;

        public TaskController(TaskService taskService, AssignService assignService)
        {
            _taskService = taskService;
            _assignService = assignService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            TaskIndexViewModel model = await _taskService.GetIndexAsync(
                currentUserId.Value,
                User.IsInRole("ADMIN"),
                cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            TaskCreateViewModel model = await _taskService.BuildCreateModelAsync(
                currentUserId.Value,
                cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TaskCreateViewModel model,
            CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                await _taskService.PopulateCreateOptionsAsync(model, currentUserId.Value, cancellationToken);
                return View(model);
            }

            TaskOperationResult result = await _taskService.CreateAsync(
                currentUserId.Value,
                model,
                cancellationToken);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                await _taskService.PopulateCreateOptionsAsync(model, currentUserId.Value, cancellationToken);
                return View(model);
            }

            TempData["TaskMessage"] = $"任务发布成功，任务编号 #{result.TaskId}，当前状态为待接单";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            TempData["TaskMessage"] = await _taskService.CancelAsync(
                id,
                currentUserId.Value,
                cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            TaskDetailsViewModel? model = await _taskService.GetDetailsAsync(
                id,
                currentUserId.Value,
                User.IsInRole("ADMIN"),
                cancellationToken);
            return model == null ? NotFound() : View(model);
        }

        [Authorize(Roles = "RUNNER")]
        [HttpGet]
        public async Task<IActionResult> Hall(
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            int? currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            TaskHallViewModel model = await _assignService.GetTaskHallAsync(
                currentUserId.Value,
                page,
                pageSize,
                cancellationToken);
            return View(model);
        }

        [Authorize(Roles = "RUNNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grab(int taskId, CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            bool success = currentUserId.HasValue && await _assignService.GrabTaskAsync(
                taskId,
                currentUserId.Value,
                cancellationToken);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "接单成功，请前往配送工作台处理任务。"
                : "接单失败。任务可能已被接走，或您的账号当前不可接单。";
            return RedirectToAction(nameof(Hall));
        }

        [Authorize(Roles = "RUNNER")]
        [HttpGet]
        public async Task<IActionResult> MyTasks(
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            int? currentUserId = GetCurrentUserId();
            MyTasksViewModel? model = currentUserId.HasValue
                ? await _assignService.GetMyTasksAsync(currentUserId.Value, page, pageSize, cancellationToken)
                : null;
            return model == null ? Forbid() : View(model);
        }

        [Authorize(Roles = "RUNNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int taskId,
            string targetStatus,
            CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            bool success = currentUserId.HasValue && await _assignService.UpdateStatusAsync(
                taskId,
                targetStatus,
                currentUserId.Value,
                cancellationToken);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "任务状态已更新。"
                : "状态更新失败，请刷新页面后重试。";
            return RedirectToAction(nameof(MyTasks));
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            int? currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            ReceiptTasksViewModel model = await _assignService.GetReceiptTasksAsync(
                currentUserId.Value,
                page,
                pageSize,
                cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceipt(int taskId, CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            bool success = currentUserId.HasValue && await _assignService.ConfirmReceiptAsync(
                taskId,
                currentUserId.Value,
                cancellationToken);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "已确认收货，任务将等待后续支付处理。"
                : "确认收货失败，请确认任务状态和发布人身份。";
            return RedirectToAction(nameof(Receipt));
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> AdminConsole(
            int taskPage = 1,
            int runnerPage = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            AdminAssignViewModel model = await _assignService.GetAdminConsoleAsync(
                taskPage,
                runnerPage,
                pageSize,
                cancellationToken);
            return View(model);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminAssign(
            int taskId,
            int runnerId,
            CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            bool success = currentUserId.HasValue && await _assignService.AssignTaskAsync(
                taskId,
                runnerId,
                currentUserId.Value,
                cancellationToken);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "任务指派成功。"
                : "任务指派失败，请检查任务和跑腿员状态。";
            return RedirectToAction(nameof(AdminConsole));
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminReassign(
            int taskId,
            int runnerId,
            string? reason,
            CancellationToken cancellationToken)
        {
            int? currentUserId = GetCurrentUserId();
            bool success = currentUserId.HasValue && await _assignService.ReassignTaskAsync(
                taskId,
                runnerId,
                reason,
                currentUserId.Value,
                cancellationToken);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "任务重派成功。"
                : "任务重派失败，请检查任务状态和新跑腿员状态。";
            return RedirectToAction(nameof(AdminConsole));
        }

        private int? GetCurrentUserId()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int parsedUserId;
            if (int.TryParse(userId, out parsedUserId))
            {
                return parsedUserId;
            }

            return null;
        }
    }
}
