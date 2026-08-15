using System.Security.Claims;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class PaymentController(IPaymentService paymentService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Confirm(int taskId, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        PaymentConfirmViewModel? model = await paymentService.BuildConfirmModelAsync(taskId, currentUserId.Value, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(PaymentConfirmViewModel model, string submitAction, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            await RebuildConfirmModelAsync(model, currentUserId.Value, cancellationToken);
            return View(model);
        }

        bool payLater = string.Equals(submitAction, "LATER", StringComparison.OrdinalIgnoreCase);
        PaymentOperationResult result = payLater
            ? await paymentService.SaveUnpaidPaymentAsync(model.TaskId, currentUserId.Value, model.PayMethod, cancellationToken)
            : await paymentService.SubmitPaymentAsync(model.TaskId, currentUserId.Value, model.PayMethod, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await RebuildConfirmModelAsync(model, currentUserId.Value, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = payLater
            ? "已保存为待付款；任务已经完成，跑腿员已恢复为可接单状态。"
            : "支付成功，任务已进入已完成状态。";
        return RedirectToAction(nameof(Status), new { paymentId = result.PaymentId });
    }

    [HttpGet]
    public async Task<IActionResult> Status(
        string? keyword,
        int? taskId,
        int? paymentId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        PaymentStatusQueryViewModel model = paymentId.HasValue || taskId.HasValue
            ? await paymentService.QueryPaymentAsync(currentUserId.Value, taskId, paymentId, cancellationToken)
            : await paymentService.GetMyPaymentStatusAsync(currentUserId.Value, keyword, page, pageSize, cancellationToken);
        return View(model);
    }

    private async Task RebuildConfirmModelAsync(PaymentConfirmViewModel model, int currentUserId, CancellationToken cancellationToken)
    {
        PaymentConfirmViewModel? source = await paymentService.BuildConfirmModelAsync(model.TaskId, currentUserId, cancellationToken);
        if (source is null)
        {
            return;
        }

        model.RecordId = source.RecordId;
        model.TaskTitle = source.TaskTitle;
        model.TaskAmount = source.TaskAmount;
        model.TaskStatusDisplayName = source.TaskStatusDisplayName;
        model.ReceiptConfirmed = source.ReceiptConfirmed;
        model.CanSubmitPayment = source.CanSubmitPayment;
    }

    private int? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }
}
