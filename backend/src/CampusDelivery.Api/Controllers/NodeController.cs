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
        return Forbid();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(NodeCreateViewModel model, CancellationToken cancellationToken)
    {
        return Forbid();
    }

    [HttpGet]
    public IActionResult Edit(int id, CancellationToken cancellationToken)
    {
        return Forbid();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(NodeEditViewModel model, CancellationToken cancellationToken)
    {
        return Forbid();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id, CancellationToken cancellationToken)
    {
        return Forbid();
    }
}
