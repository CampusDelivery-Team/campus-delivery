using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class NodeController(INodeService nodeService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await nodeService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = nameof(NodeIndexViewModel.CreateModel))]
        NodeCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ViewIndexAsync(
                createModel: model,
                cancellationToken: cancellationToken);
        }

        if (!await nodeService.CreateAsync(model, cancellationToken))
        {
            ModelState.AddModelError(
                $"{nameof(NodeIndexViewModel.CreateModel)}.{nameof(model.NodeName)}",
                "节点名称已存在，请使用其他名称");
            return await ViewIndexAsync(
                createModel: model,
                cancellationToken: cancellationToken);
        }

        TempData["NodeMessage"] = "节点已新增";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Bind(Prefix = nameof(NodeIndexViewModel.EditModel))]
        NodeEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ViewIndexAsync(
                editModel: model,
                cancellationToken: cancellationToken);
        }

        var result = await nodeService.UpdateAsync(model, cancellationToken);
        if (result == NodeUpdateResult.DuplicateName)
        {
            ModelState.AddModelError(
                $"{nameof(NodeIndexViewModel.EditModel)}.{nameof(model.NodeName)}",
                "节点名称已存在，请使用其他名称");
            return await ViewIndexAsync(
                editModel: model,
                cancellationToken: cancellationToken);
        }

        if (result == NodeUpdateResult.NotFound)
        {
            ModelState.AddModelError(string.Empty, "节点不存在或已被移除");
            return await ViewIndexAsync(
                editModel: model,
                cancellationToken: cancellationToken);
        }

        TempData["NodeMessage"] = "节点资料已更新";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        string status,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || status is not ("NORMAL" or "CLOSED"))
        {
            return BadRequest();
        }

        var result = await nodeService.UpdateStatusAsync(id, status, cancellationToken);
        TempData["NodeMessage"] = result switch
        {
            NodeStatusUpdateResult.Success when status == "NORMAL" => "节点已恢复正常",
            NodeStatusUpdateResult.Success => "节点已关闭",
            NodeStatusUpdateResult.NotFound => "节点不存在",
            _ => "节点状态没有变化"
        };
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var result = await nodeService.DeleteAsync(id, cancellationToken);
        TempData["NodeMessage"] = result switch
        {
            NodeDeleteOperationResult.Success => "节点已删除",
            NodeDeleteOperationResult.Referenced => "该节点已有任务记录，不能删除；可先关闭节点",
            _ => "节点不存在或已被删除"
        };
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ViewIndexAsync(
        CancellationToken cancellationToken,
        NodeCreateViewModel? createModel = null,
        NodeEditViewModel? editModel = null)
    {
        var indexModel = await nodeService.GetIndexAsync(cancellationToken);
        if (createModel is not null)
        {
            indexModel.CreateModel = createModel;
        }

        if (editModel is not null)
        {
            indexModel.EditModel = editModel;
        }

        return View(nameof(Index), indexModel);
    }
}
