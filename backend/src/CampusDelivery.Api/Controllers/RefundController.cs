using System.Security.Claims;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class RefundController : Controller
{
    private readonly IRefundService _refundService;

    public RefundController(IRefundService refundService)
    {
        _refundService = refundService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int taskId, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        RefundCreateViewModel? model = await _refundService.BuildCreateModelAsync(taskId, currentUserId.Value, cancellationToken);
        if (model is null)
        {
            TempData["ErrorMessage"] = "当前订单暂不支持在线退款。如订单已进入结算流程，请在任务详情页提交投诉，管理员会进行核查处理。";
            return RedirectToAction("Status", "Payment", new { taskId });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RefundCreateViewModel model, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            await RebuildCreateModelAsync(model, currentUserId.Value, cancellationToken);
            return View(model);
        }

        RefundOperationResult result = await _refundService.SubmitAsync(model, currentUserId.Value, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await RebuildCreateModelAsync(model, currentUserId.Value, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "退款申请已提交，请等待管理员审核。";
        return RedirectToAction("Status", "Payment", new { taskId = model.TaskId });
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> AdminIndex(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        RefundAdminListViewModel model = await _refundService.GetAdminListAsync(page, pageSize, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Review(int refundId, CancellationToken cancellationToken)
    {
        RefundReviewViewModel? model = await _refundService.GetReviewModelAsync(refundId, cancellationToken);
        return model == null ? NotFound() : View(model);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(RefundReviewViewModel model, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            await RebuildReviewModelAsync(model, cancellationToken);
            return View(model);
        }

        RefundOperationResult result = await _refundService.ReviewAsync(
            model.RefundId,
            model.Decision,
            model.ReviewReason,
            currentUserId.Value,
            cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await RebuildReviewModelAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = model.Decision == "APPROVED"
            ? "退款已通过，支付状态已更新为已退款。"
            : "退款已拒绝。";
        return RedirectToAction(nameof(AdminIndex));
    }

    private async Task RebuildCreateModelAsync(RefundCreateViewModel model, int currentUserId, CancellationToken cancellationToken)
    {
        RefundCreateViewModel? rebuild = await _refundService.BuildCreateModelAsync(model.TaskId, currentUserId, cancellationToken);
        if (rebuild is null)
        {
            return;
        }

        model.TaskTitle = rebuild.TaskTitle;
        model.RefundAmount = rebuild.RefundAmount;
        model.PayStatusDisplayName = rebuild.PayStatusDisplayName;
        model.RecordId = rebuild.RecordId;
        model.PaymentId = rebuild.PaymentId;
    }

    private async Task RebuildReviewModelAsync(RefundReviewViewModel model, CancellationToken cancellationToken)
    {
        RefundReviewViewModel? rebuild = await _refundService.GetReviewModelAsync(model.RefundId, cancellationToken);
        if (rebuild is null)
        {
            return;
        }

        model.PaymentId = rebuild.PaymentId;
        model.TaskId = rebuild.TaskId;
        model.TaskTitle = rebuild.TaskTitle;
        model.RefundAmount = rebuild.RefundAmount;
        model.RefundReason = rebuild.RefundReason;
    }

    private int? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }
}
