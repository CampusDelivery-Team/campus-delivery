using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class ServiceNodeRuleController(IServiceNodeRuleService ruleService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await ruleService.GetIndexAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = nameof(ServiceNodeRuleIndexViewModel.CreateModel))]
        ServiceNodeRuleCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ViewIndexAsync(model, cancellationToken);
        }

        var result = await ruleService.CreateAsync(model, cancellationToken);
        if (result == ServiceNodeRuleCreateResult.Duplicate)
        {
            ModelState.AddModelError(string.Empty, "该服务类型已经绑定了所选节点");
            return await ViewIndexAsync(model, cancellationToken);
        }

        if (result == ServiceNodeRuleCreateResult.Unavailable)
        {
            ModelState.AddModelError(string.Empty, "只能绑定启用的服务类型和状态正常的节点");
            return await ViewIndexAsync(model, cancellationToken);
        }

        TempData["ServiceNodeRuleMessage"] = "服务节点绑定已新增";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        int serviceTypeId,
        int nodeId,
        CancellationToken cancellationToken)
    {
        if (serviceTypeId <= 0 || nodeId <= 0)
        {
            return BadRequest();
        }

        var result = await ruleService.RemoveAsync(serviceTypeId, nodeId, cancellationToken);
        TempData["ServiceNodeRuleMessage"] = result switch
        {
            ServiceNodeRuleRemoveResult.Success => "服务节点绑定已解除",
            ServiceNodeRuleRemoveResult.Referenced => "该绑定已被历史任务使用，不能解除",
            _ => "指定的服务节点绑定不存在"
        };

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ViewIndexAsync(
        ServiceNodeRuleCreateViewModel createModel,
        CancellationToken cancellationToken)
    {
        var indexModel = await ruleService.GetIndexAsync(cancellationToken);
        indexModel.CreateModel = createModel;
        return View(nameof(Index), indexModel);
    }
}
