using System.Security.Claims;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class ReviewController(ReviewService reviewService) : Controller
{
    [HttpGet]
    public IActionResult Index(int recordId)
    {
        ReviewListItemViewModel? review = reviewService.GetByRecordId(recordId);
        ViewBag.RecordId = recordId;
        return View(review);
    }

    [HttpGet]
    public IActionResult Create(int recordId)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue || !reviewService.CanReview(recordId, currentUserId.Value))
        {
            return Forbid();
        }
        return View(new ReviewCreateViewModel { RecordId = recordId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ReviewCreateViewModel model)
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

        var result = reviewService.Add(model, currentUserId.Value);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(model);
        }

        TempData["SuccessMessage"] = "评价提交成功。";
        return RedirectToAction(nameof(Index), new { recordId = model.RecordId });
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public IActionResult All(int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = reviewService.GetTotalCount();
        return View(reviewService.GetAll(page, pageSize));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public IActionResult Edit(int id)
    {
        ReviewEditViewModel? model = reviewService.GetEditModel(id);
        return model == null ? NotFound() : View(model);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ReviewEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = reviewService.Update(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(model);
        }

        TempData["SuccessMessage"] = "评价已更新，跑腿员信誉分已同步调整。";
        return RedirectToAction(nameof(All));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var result = reviewService.Delete(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "评价已删除，跑腿员信誉分已恢复。"
            : result.ErrorMessage;
        return RedirectToAction(nameof(All));
    }

    private int? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }
}
