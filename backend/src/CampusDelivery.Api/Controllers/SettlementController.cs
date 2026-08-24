using System.Security.Claims;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class SettlementController(ISettlementService settlementService) : Controller
{
    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await settlementService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Candidates(CancellationToken cancellationToken)
    {
        var model = await settlementService.GetCandidatesAsync(cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await settlementService.GetDetailsAsync(id, cancellationToken);
        return model == null ? NotFound() : View(model);
    }

    [Authorize(Roles = "RUNNER")]
    [HttpGet]
    public async Task<IActionResult> My(CancellationToken cancellationToken)
    {
        var model = await settlementService.GetRunnerSettlementsAsync(GetCurrentUserId(), cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "RUNNER")]
    [HttpGet]
    public async Task<IActionResult> MyDetails(int id, CancellationToken cancellationToken)
    {
        var model = await settlementService.GetRunnerSettlementDetailsAsync(
            id,
            GetCurrentUserId(),
            cancellationToken);

        return model == null ? NotFound() : View(model);
    }

    [Authorize(Roles = "ADMIN")]
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

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int settlementId, string status, CancellationToken cancellationToken)
    {
        SettlementOperationResult result = await settlementService.ChangeStatusAsync(settlementId, status, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = settlementId });
    }

    private int GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out int userId) || userId <= 0)
        {
            throw new InvalidOperationException("当前登录信息缺少有效用户编号。");
        }

        return userId;
    }
}
