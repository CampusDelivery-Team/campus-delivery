using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class ReportController(IReportService reportService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await reportService.GetDashboardAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(ReportGenerateViewModel model, CancellationToken cancellationToken)
    {
        ReportOperationResult result = await reportService.GenerateAsync(model, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return result.Success && result.ReportId.HasValue
            ? RedirectToAction(nameof(Details), new { id = result.ReportId.Value })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ReportDetailsViewModel? model = await reportService.GetDetailsAsync(id, cancellationToken);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Export(int id, CancellationToken cancellationToken)
    {
        ReportExportResult result = await reportService.ExportAsync(id, cancellationToken);
        if (!result.Success || result.Content == null || result.FileName == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        return File(result.Content, "text/csv; charset=utf-8", result.FileName);
    }
}

