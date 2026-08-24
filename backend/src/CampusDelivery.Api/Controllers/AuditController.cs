using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class AuditController(IAuditService auditService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await auditService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string auditObject = "PAYMENT", CancellationToken cancellationToken = default)
    {
        var model = await auditService.BuildCreateModelAsync(auditObject, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuditCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            AuditCreateViewModel rebuilt = await auditService.BuildCreateModelAsync(model.AuditObject, cancellationToken);
            model.Targets = rebuilt.Targets;
            model.AuditObjectDisplayName = rebuilt.AuditObjectDisplayName;
            return View(model);
        }

        AuditOperationResult result = await auditService.CreateAuditAsync(model, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        if (!result.Success)
        {
            AuditCreateViewModel rebuilt = await auditService.BuildCreateModelAsync(model.AuditObject, cancellationToken);
            model.Targets = rebuilt.Targets;
            model.AuditObjectDisplayName = rebuilt.AuditObjectDisplayName;
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }
}

