using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class ServiceTypeController(ServiceTypeService serviceTypeService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await serviceTypeService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = nameof(ServiceTypeIndexViewModel.CreateModel))]
        ServiceTypeCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ViewIndexAsync(
                createModel: model,
                cancellationToken: cancellationToken);
        }

        var created = await serviceTypeService.CreateAsync(model, cancellationToken);
        if (!created)
        {
            ModelState.AddModelError(
                $"{nameof(ServiceTypeIndexViewModel.CreateModel)}.{nameof(model.ServiceName)}",
                "服务名称已存在，请使用其他名称");
            return await ViewIndexAsync(
                createModel: model,
                cancellationToken: cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Bind(Prefix = nameof(ServiceTypeIndexViewModel.EditModel))]
        ServiceTypeEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ViewIndexAsync(
                editModel: model,
                cancellationToken: cancellationToken);
        }

        var result = await serviceTypeService.UpdateAsync(model, cancellationToken);
        if (result == ServiceTypeUpdateResult.DuplicateName)
        {
            ModelState.AddModelError(
                $"{nameof(ServiceTypeIndexViewModel.EditModel)}.{nameof(model.ServiceName)}",
                "服务名称已存在，请使用其他名称");
            return await ViewIndexAsync(
                editModel: model,
                cancellationToken: cancellationToken);
        }

        if (result == ServiceTypeUpdateResult.NotFound)
        {
            ModelState.AddModelError(string.Empty, "服务类型不存在或已被移除");
            return await ViewIndexAsync(
                editModel: model,
                cancellationToken: cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        string status,
        CancellationToken cancellationToken)
    {
        if (status is not ("ENABLED" or "DISABLED"))
        {
            return BadRequest();
        }

        await serviceTypeService.UpdateStatusAsync(id, status, cancellationToken);
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

        var result = await serviceTypeService.DeleteAsync(id, cancellationToken);
        TempData["ServiceTypeMessage"] = result switch
        {
            ServiceTypeDeleteResult.Success => "服务类型已删除",
            ServiceTypeDeleteResult.Referenced => "该服务类型已有任务记录，不能删除；可先停用服务",
            _ => "服务类型不存在或已被删除"
        };
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ViewIndexAsync(
        CancellationToken cancellationToken,
        ServiceTypeCreateViewModel? createModel = null,
        ServiceTypeEditViewModel? editModel = null)
    {
        var indexModel = await serviceTypeService.GetIndexAsync(cancellationToken);
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
