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

        public TaskController(TaskService taskService)
        {
            _taskService = taskService;
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
