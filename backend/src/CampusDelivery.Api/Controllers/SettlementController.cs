using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class SettlementController(SettlementService settlementService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await settlementService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Candidates(CancellationToken cancellationToken)
    {
        var model = await settlementService.GetCandidatesAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await settlementService.GetDetailsAsync(id, cancellationToken);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateForRunner(int runnerId, CancellationToken cancellationToken)
    {
        SettlementOperationResult result = await settlementService.GenerateForRunnerAsync(runnerId, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return result.Success && result.SettlementId.HasValue
            ? RedirectToAction(nameof(Details), new { id = result.SettlementId.Value })
            : RedirectToAction(nameof(Candidates));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int settlementId, string status, CancellationToken cancellationToken)
    {
        SettlementOperationResult result = await settlementService.ChangeStatusAsync(settlementId, status, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = settlementId });
    }
}

