using System.Security.Claims;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize]
public sealed class PaymentController : Controller
{
    private readonly PaymentService _paymentService;

    public PaymentController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int taskId, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        PaymentConfirmViewModel? model = await _paymentService.BuildConfirmModelAsync(taskId, currentUserId.Value, cancellationToken);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(PaymentConfirmViewModel model, CancellationToken cancellationToken)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            PaymentConfirmViewModel? confirmModel = await _paymentService.BuildConfirmModelAsync(model.TaskId, currentUserId.Value, cancellationToken);
            if (confirmModel != null)
            {
                model.TaskTitle = confirmModel.TaskTitle;
                model.TaskAmount = confirmModel.TaskAmount;
                model.TaskStatusDisplayName = confirmModel.TaskStatusDisplayName;
                model.ReceiptConfirmed = confirmModel.ReceiptConfirmed;
                model.CanSubmitPayment = confirmModel.CanSubmitPayment;
            }

            return View(model);
        }

        PaymentOperationResult result = await _paymentService.SubmitPaymentAsync(
            model.TaskId,
            currentUserId.Value,
            model.PayMethod,
            model.ThirdTradeNo,
            cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            PaymentConfirmViewModel? confirmModel = await _paymentService.BuildConfirmModelAsync(model.TaskId, currentUserId.Value, cancellationToken);
            if (confirmModel != null)
            {
                model.TaskTitle = confirmModel.TaskTitle;
                model.TaskAmount = confirmModel.TaskAmount;
                model.TaskStatusDisplayName = confirmModel.TaskStatusDisplayName;
                model.ReceiptConfirmed = confirmModel.ReceiptConfirmed;
                model.CanSubmitPayment = confirmModel.CanSubmitPayment;
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "支付成功，任务已进入已完成状态。";
        return RedirectToAction(nameof(Status), new { taskId = model.TaskId });
    }

    [HttpGet]
    public async Task<IActionResult> Status(int? taskId, int? paymentId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        int? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        PaymentStatusQueryViewModel model = paymentId.HasValue || taskId.HasValue
            ? await _paymentService.QueryPaymentAsync(currentUserId.Value, taskId, paymentId, cancellationToken)
            : await _paymentService.GetMyPaymentStatusAsync(currentUserId.Value, page, pageSize, cancellationToken);
        return View(model);
    }

    private int? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }
}
