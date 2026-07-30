using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class ReviewController(ReviewService reviewService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<IActionResult> Index(int taskId, CancellationToken cancellationToken)
    {
        var reviews = await reviewService.GetByTaskIdAsync(taskId, cancellationToken);
        var items = reviews.Select(ReviewListItemViewModel.FromModel).ToList();
        ViewBag.TaskId = taskId;
        return View(items);
    }

    [HttpGet]
    public IActionResult Create(int recordId)
    {
        return View(new ReviewCreateViewModel { RecordId = recordId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await reviewService.CreateReviewAsync(
            model.RecordId, model.Rating ?? 5, model.AnonymousFlag,
            model.CommentText, model.CreditDelta, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return success ? RedirectToAction(nameof(MyReviews)) : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyReviews(int page = 1, int size = 10, CancellationToken cancellationToken = default)
    {
        var (items, total) = await reviewService.GetAllPagedAsync(page, size, cancellationToken);
        var viewItems = items.Select(ReviewListItemViewModel.FromModel).ToList();
        ViewBag.Total = total; ViewBag.Page = page; ViewBag.Size = size;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
        return View(viewItems);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> All(int page = 1, int size = 20, CancellationToken cancellationToken = default)
    {
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
            model.CommentText, model.CreditDelta, cancellationToken);
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
