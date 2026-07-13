using System.Security.Claims;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class RunnerController(RunnerService runnerService) : Controller
{
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await runnerService.GetIndexAsync(cancellationToken: cancellationToken));
    }

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        return View(await runnerService.GetIndexAsync(
            pendingOnly: true,
            cancellationToken: cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Apply(CancellationToken cancellationToken)
    {
        return View(await runnerService.GetApplicationAsync(
            GetCurrentUserId(),
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        [Bind(Prefix = nameof(RunnerApplicationPageViewModel.Form))]
        RunnerApplicationFormViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!ModelState.IsValid)
        {
            return await ViewApplicationAsync(userId, model, cancellationToken);
        }

        var result = await runnerService.SubmitApplicationAsync(userId, model, cancellationToken);
        if (result != RunnerApplicationResult.Success)
        {
            ModelState.AddModelError(
                string.Empty,
                result == RunnerApplicationResult.AlreadyExists
                    ? "当前申请已经提交或审核完成，不能重复提交"
                    : "当前账号状态不允许申请跑腿员资格");
            return await ViewApplicationAsync(userId, model, cancellationToken);
        }

        TempData["RunnerApplicationMessage"] = "跑腿员申请已提交，请等待管理员审核";
        return RedirectToAction(nameof(Apply));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(
        int id,
        string decision,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || decision is not ("APPROVED" or "REJECTED"))
        {
            return BadRequest();
        }

        var result = await runnerService.ReviewAsync(id, decision, cancellationToken);
        TempData["RunnerMessage"] = result switch
        {
            RunnerReviewResult.Success when decision == "APPROVED" => "申请已通过，账号角色已更新为跑腿员",
            RunnerReviewResult.Success => "申请已拒绝",
            RunnerReviewResult.NotFound => "跑腿员申请不存在",
            RunnerReviewResult.AlreadyReviewed => "该申请已经处理，不能重复审核",
            _ => "关联账号已停用或不允许转换为跑腿员"
        };

        return RedirectToAction(nameof(Pending));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeWorkStatus(
        int id,
        string status,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || status is not ("FREE" or "OFFLINE"))
        {
            return BadRequest();
        }

        var result = await runnerService.UpdateWorkStatusAsync(id, status, cancellationToken);
        TempData["RunnerMessage"] = result switch
        {
            RunnerWorkStatusResult.Success => status == "FREE" ? "跑腿员已设为可接单" : "跑腿员已设为离线",
            RunnerWorkStatusResult.Busy => "跑腿员正在配送，不能手动修改工作状态",
            RunnerWorkStatusResult.NotFound => "跑腿员不存在",
            _ => "只有审核通过且当前不忙碌的跑腿员可以修改工作状态"
        };

        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId) || userId <= 0)
        {
            throw new InvalidOperationException("当前登录信息缺少有效用户编号");
        }

        return userId;
    }

    private async Task<IActionResult> ViewApplicationAsync(
        int userId,
        RunnerApplicationFormViewModel form,
        CancellationToken cancellationToken)
    {
        var pageModel = await runnerService.GetApplicationAsync(userId, cancellationToken);
        pageModel.Form = form;
        return View(nameof(Apply), pageModel);
    }
}
