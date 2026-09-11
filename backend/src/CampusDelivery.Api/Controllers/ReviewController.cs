using System.Security.Claims;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class ReviewController(
    IReviewService reviewService,
    IRunnerRepository runnerRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int taskId, CancellationToken cancellationToken)
    {
        if (taskId <= 0)
        {
            return BadRequest();
        }

        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        IReadOnlyList<Review> reviews = await reviewService.GetByTaskIdAsync(taskId, cancellationToken);
        bool isAdmin = User.IsInRole("ADMIN");
        var items = reviews
            .Select(review => ReviewListItemViewModel.FromModel(
                review,
                isAdmin || review.PublisherUserId == currentUserId.Value))
            .ToList();
        ViewBag.TaskId = taskId;
        return View(items);
    }

    [HttpGet]
    public IActionResult Create(int taskId)
    {
        if (taskId <= 0)
        {
            return BadRequest();
        }

        return View(new ReviewCreateViewModel { TaskId = taskId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ReviewCreateViewModel model,
        CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, message) = await reviewService.CreateReviewAsync(
            model.TaskId,
            model.Rating!.Value,
            model.AnonymousFlag,
            model.CommentText,
            currentUserId.Value,
            cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(MyReviews));
    }

    [HttpGet]
    public async Task<IActionResult> MyReviews(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var (items, total) = await reviewService.GetMyReviewsAsync(
            currentUserId.Value,
            page,
            pageSize,
            cancellationToken);
        var viewItems = items
            .Select(review => ReviewListItemViewModel.FromModel(review, canManage: true))
            .ToList();
        SetPaginationViewData(page, pageSize, total, defaultPageSize: 10);
        return View(viewItems);
    }

    [Authorize(Roles = "RUNNER")]
    [HttpGet]
    public async Task<IActionResult> Received(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        Runner? runner = await runnerRepository.GetByUserIdAsync(currentUserId.Value, cancellationToken);
        if (runner == null)
        {
            return Forbid();
        }

        var (items, total) = await reviewService.GetReceivedReviewsAsync(
            runner.RunnerId,
            page,
            pageSize,
            cancellationToken);
        var viewItems = items
            .Select(review => ReviewListItemViewModel.FromModel(review, canManage: false))
            .ToList();
        SetPaginationViewData(page, pageSize, total, defaultPageSize: 10);
        return View(viewItems);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> All(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await reviewService.GetAllPagedAsync(
            page,
            pageSize,
            cancellationToken);
        var viewItems = items
            .Select(review => ReviewListItemViewModel.FromModel(review, canManage: true))
            .ToList();
        SetPaginationViewData(page, pageSize, total, defaultPageSize: 20);
        return View(viewItems);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        Review? review = await reviewService.GetEditableReviewAsync(
            id,
            currentUserId.Value,
            User.IsInRole("ADMIN"),
            cancellationToken);
        if (review == null)
        {
            TempData["ErrorMessage"] = "评价不存在或你没有权限修改该评价";
            return RedirectToReviewList();
        }

        return View(ReviewEditViewModel.FromModel(review));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ReviewEditViewModel model,
        CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        bool isAdmin = User.IsInRole("ADMIN");
        var (success, message) = await reviewService.UpdateReviewAsync(
            model.ReviewId,
            model.Rating!.Value,
            model.AnonymousFlag,
            model.CommentText,
            currentUserId.Value,
            isAdmin,
            cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToReviewList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var (success, message) = await reviewService.DeleteReviewAsync(
            id,
            currentUserId.Value,
            User.IsInRole("ADMIN"),
            cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToReviewList();
    }

    private int? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) && userId > 0 ? userId : null;
    }

    private IActionResult RedirectToReviewList() => User.IsInRole("ADMIN")
        ? RedirectToAction(nameof(All))
        : RedirectToAction(nameof(MyReviews));

    private void SetPaginationViewData(int page, int pageSize, int total, int defaultPageSize)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 50 ? pageSize : defaultPageSize;
        int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        ViewBag.Total = total;
        ViewBag.Page = Math.Min(page, totalPages);
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = totalPages;
    }
}
