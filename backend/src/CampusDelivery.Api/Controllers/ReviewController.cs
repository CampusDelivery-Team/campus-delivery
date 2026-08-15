using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class ReviewController(ReviewService reviewService, TaskService taskService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<IActionResult> Index(int taskId, CancellationToken cancellationToken)
    {
        var task = await taskService.GetDetailsAsync(
            taskId, CurrentUserId, User.IsInRole("ADMIN"), cancellationToken);
        if (task == null)
        {
            return NotFound();
        }

        var reviews = await reviewService.GetByTaskIdAsync(taskId, cancellationToken);
        var items = reviews.Select(ReviewListItemViewModel.FromModel).ToList();
        ViewBag.TaskId = taskId;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int recordId, CancellationToken cancellationToken)
    {
        if (!await reviewService.CanCreateReviewAsync(recordId, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        return View(new ReviewCreateViewModel { RecordId = recordId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await reviewService.CreateReviewAsync(
            model.RecordId, model.Rating ?? 5, model.AnonymousFlag,
            model.CommentText, CurrentUserId, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return success ? RedirectToAction(nameof(MyReviews)) : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyReviews(int page = 1, int size = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        size = size is >= 1 and <= 50 ? size : 10;
        var (items, total) = await reviewService.GetMyPagedAsync(CurrentUserId, page, size, cancellationToken);
        var viewItems = items.Select(ReviewListItemViewModel.FromModel).ToList();
        ViewBag.Total = total; ViewBag.Page = page; ViewBag.Size = size;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
        return View(viewItems);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> All(int page = 1, int size = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        size = size is >= 1 and <= 50 ? size : 20;
        var (items, total) = await reviewService.GetAllPagedAsync(page, size, cancellationToken);
        var viewItems = items.Select(ReviewListItemViewModel.FromModel).ToList();
        ViewBag.Total = total; ViewBag.Page = page; ViewBag.Size = size;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
        return View(viewItems);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var review = await reviewService.GetByIdAsync(id, cancellationToken);
        if (review == null) return NotFound();
        return View(ReviewEditViewModel.FromModel(review));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReviewEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await reviewService.UpdateReviewAsync(
            model.ReviewId, model.Rating ?? 5, model.AnonymousFlag,
            model.CommentText, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return success ? RedirectToAction(nameof(All)) : View(model);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (success, message) = await reviewService.DeleteReviewAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(All));
    }
}
