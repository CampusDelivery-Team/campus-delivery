using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

public sealed class NodeController(NodeService nodeService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await nodeService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new NodeCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NodeCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await nodeService.CreateAsync(model, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await nodeService.GetEditModelAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NodeEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await nodeService.UpdateAsync(model, cancellationToken);
        return updated ? RedirectToAction(nameof(Index)) : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await nodeService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
