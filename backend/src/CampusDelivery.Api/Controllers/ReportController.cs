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
        return RedirectToAction(nameof(Index));
    }
}

