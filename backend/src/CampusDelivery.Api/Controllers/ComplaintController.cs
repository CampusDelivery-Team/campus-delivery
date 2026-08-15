using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class ComplaintController(IComplaintService complaintService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public IActionResult Create(int recordId)
    {
        return View(new ComplaintCreateViewModel { RecordId = recordId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplaintCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await complaintService.CreateComplaintAsync(
            model.RecordId, model.Reason, CurrentUserId, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return success ? RedirectToAction(nameof(MyComplaints)) : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyComplaints(int page = 1, int size = 10, CancellationToken cancellationToken = default)
    {
        var (items, total) = await complaintService.GetMyComplaintsAsync(CurrentUserId, page, size, cancellationToken);
        var viewItems = items.Select(ComplaintListItemViewModel.FromModel).ToList();
        ViewBag.Total = total; ViewBag.Page = page; ViewBag.Size = size;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
        return View(viewItems);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int size = 20, CancellationToken cancellationToken = default)
    {
        var (items, total) = await complaintService.GetAllPagedAsync(page, size, cancellationToken);
        var viewItems = items.Select(ComplaintListItemViewModel.FromModel).ToList();
        ViewBag.Total = total; ViewBag.Page = page; ViewBag.Size = size;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
        return View(viewItems);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Process(int id, CancellationToken cancellationToken)
    {
        var complaint = await complaintService.GetByIdAsync(id, cancellationToken);
        if (complaint == null) return NotFound();
        return View(ComplaintProcessViewModel.FromModel(complaint));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(ComplaintProcessViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await complaintService.ProcessComplaintAsync(
            model.ComplaintId, model.Decision, model.ProcessNote, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return success ? RedirectToAction(nameof(Index)) : View(model);
    }
}
