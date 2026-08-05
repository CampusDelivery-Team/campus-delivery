using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class PaymentService(
    TaskRepository taskRepository,
    PaymentRepository paymentRepository,
    RefundRepository refundRepository,
    OracleConnectionFactory connectionFactory)
{
    private const string ReviewReasonMarker = "\n审核意见：";

    public async Task<PaymentConfirmViewModel?> BuildConfirmModelAsync(int taskId, int currentUserId, CancellationToken cancellationToken = default)
    {
        TaskDetailsRecord? details = await taskRepository.GetDetailsAsync(taskId, currentUserId, false, cancellationToken);
        if (details?.RecordId is null)
        {
            return null;
        }

        PaymentRecord? payment = await paymentRepository.GetByTaskIdAsync(taskId, cancellationToken);
        bool receiptConfirmed = (await taskRepository.GetStatusLogsByTaskIdAsync(taskId, cancellationToken))
            .Any(log => log.StatusBefore == "WAIT_CONFIRM" && log.StatusAfter == "WAIT_CONFIRM" && log.OperatorUserId == currentUserId);

        return new()
        {
            TaskId = taskId,
            RecordId = details.RecordId.Value,
            TaskTitle = details.Task.TaskTitle,
            TaskAmount = details.Task.TaskPrice,
            TaskStatusDisplayName = DisplayNameService.GetTaskStatusName(details.Task.TaskStatus),
            ReceiptConfirmed = receiptConfirmed,
            CanSubmitPayment = (details.Task.TaskStatus == "WAIT_CONFIRM"
                    || details.Task.TaskStatus == "FINISHED" && payment?.PayStatus == "UNPAID")
                && receiptConfirmed
                && payment?.PayStatus is not "PAID" and not "REFUNDED",
            PayMethod = payment?.PayMethod ?? "WECHAT"
        };
    }

    public Task<PaymentOperationResult> SubmitPaymentAsync(
        int taskId, int currentUserId, string payMethod, CancellationToken cancellationToken = default) =>
        SavePaymentAsync(taskId, currentUserId, payMethod, completePayment: true, cancellationToken);

    public Task<PaymentOperationResult> SaveUnpaidPaymentAsync(
        int taskId, int currentUserId, string payMethod, CancellationToken cancellationToken = default) =>
        SavePaymentAsync(taskId, currentUserId, payMethod, completePayment: false, cancellationToken);

    private async Task<PaymentOperationResult> SavePaymentAsync(
        int taskId,
        int currentUserId,
        string payMethod,
        bool completePayment,
        CancellationToken cancellationToken)
    {
        if (payMethod is not ("WECHAT" or "ALIPAY" or "CASH"))
        {
            return new(false, "支付方式无效。", 0);
        }

        TaskDetailsRecord? details = await taskRepository.GetDetailsAsync(taskId, currentUserId, false, cancellationToken);
        if (details?.RecordId is null)
        {
            return new(false, "任务不存在或无权操作。", 0);
        }

        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            string? taskStatus = await taskRepository.GetTaskStatusWithLockAsync(taskId, connection, transaction, cancellationToken);
            int? publisherUserId = await taskRepository.GetTaskPublisherUserIdAsync(taskId, connection, transaction, cancellationToken);
            AssignRecord? assignRecord = await taskRepository.GetLatestAssignRecordWithConnectionAsync(taskId, connection, transaction, cancellationToken);
            PaymentRecord? existing = await paymentRepository.GetByTaskIdWithLockAsync(taskId, connection, transaction, cancellationToken);
            bool isInitialReceiptSettlement = taskStatus == "WAIT_CONFIRM";
            bool isDeferredPayment = taskStatus == "FINISHED" && existing?.PayStatus == "UNPAID";
            if ((!isInitialReceiptSettlement && !isDeferredPayment)
                || publisherUserId != currentUserId
                || assignRecord is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "只有已确认收货的任务，或已保存的待付款记录可以继续付款。", 0);
            }

            if (!await taskRepository.IsReceiptConfirmedAsync(assignRecord.RecordId, currentUserId, connection, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "请先确认收货。", 0);
            }

            if (existing?.PayStatus is "PAID" or "REFUNDED")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "该任务已经支付或退款，不能重复操作。", existing.PaymentId);
            }

            PaymentRecord payment = new()
            {
                RecordId = assignRecord.RecordId,
                OrderAmount = details.Task.TaskPrice,
                PayAmount = details.Task.TaskPrice,
                PayMethod = payMethod,
                ThirdTradeNo = null,
                PayStatus = completePayment ? "PAID" : "UNPAID"
            };

            int paymentId = existing is null
                ? await paymentRepository.InsertAsync(payment, connection, transaction, cancellationToken)
                : existing.PaymentId;
            if (existing is not null)
            {
                await paymentRepository.UpdatePaymentAsync(paymentId, payment, connection, transaction, cancellationToken);
            }

            if (isInitialReceiptSettlement)
            {
                Runner? runner = await taskRepository.GetRunnerWithLockAsync(assignRecord.RunnerId, connection, transaction, cancellationToken);
                if (runner is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new(false, "接单跑腿员不存在。", 0);
                }

                await taskRepository.UpdateTaskStatusAsync(taskId, "FINISHED", connection, transaction, cancellationToken);
                await taskRepository.UpdateRunnerWorkStatusAsync(assignRecord.RunnerId, "FREE", connection, transaction, cancellationToken);
                await taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
                {
                    RecordId = assignRecord.RecordId,
                    StatusBefore = "WAIT_CONFIRM",
                    StatusAfter = "FINISHED",
                    OperatorUserId = currentUserId
                }, connection, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new(true, string.Empty, paymentId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentStatusQueryViewModel> GetMyPaymentStatusAsync(
        int currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 20 ? pageSize : 10;
        int count = await paymentRepository.GetPaymentsCountByPublisherUserIdAsync(currentUserId, keyword, cancellationToken);
        int totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Min(page, totalPages);
        IReadOnlyList<PaymentListRecord> payments = await paymentRepository.GetPaymentsByPublisherUserIdAsync(
            currentUserId, keyword, (page - 1) * pageSize, pageSize, cancellationToken);
        return new()
        {
            Keyword = keyword,
            QueryText = keyword is null ? "我的支付记录" : $"标题包含“{keyword}”的支付记录",
            Payments = payments.Select(MapSummary).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = count,
            TotalPages = totalPages
        };
    }

    public async Task<PaymentStatusQueryViewModel> QueryPaymentAsync(
        int currentUserId, int? taskId, int? paymentId, CancellationToken cancellationToken = default)
    {
        PaymentRecord? payment = paymentId.HasValue
            ? await paymentRepository.GetByIdAsync(paymentId.Value, cancellationToken)
            : taskId.HasValue ? await paymentRepository.GetByTaskIdAsync(taskId.Value, cancellationToken) : null;
        if (payment is null || payment.PublisherUserId != currentUserId)
        {
            return new() { TaskId = taskId, PaymentId = paymentId };
        }

        TaskDetailsRecord? details = await taskRepository.GetDetailsAsync(payment.TaskId, currentUserId, false, cancellationToken);
        RefundRecord? refund = await refundRepository.GetByPaymentIdAsync(payment.PaymentId, cancellationToken);
        PaymentSummaryViewModel summary = MapSummary(payment, details?.Task.TaskTitle ?? string.Empty);
        summary.RefundProcessStatus = refund?.ProcessStatus;
        summary.RefundStatusDisplayName = refund is null ? null : DisplayNameService.GetRefundStatusName(refund.ProcessStatus);
        if (refund is not null)
        {
            (summary.RefundReason, summary.RefundReviewReason) = SplitRefundReasons(refund.RefundReason);
        }
        return new()
        {
            TaskId = payment.TaskId,
            PaymentId = payment.PaymentId,
            QueryText = details?.Task.TaskTitle ?? $"支付记录 #{payment.PaymentId}",
            Payment = summary
        };
    }

    private static PaymentSummaryViewModel MapSummary(PaymentListRecord payment) => new()
    {
        PaymentId = payment.PaymentId,
        TaskId = payment.TaskId,
        RecordId = payment.RecordId,
        TaskTitle = payment.TaskTitle,
        OrderAmount = payment.OrderAmount,
        PayAmount = payment.PayAmount,
        PayMethodDisplayName = DisplayNameService.GetPayMethodName(payment.PayMethod),
        PayStatusDisplayName = DisplayNameService.GetPayStatusName(payment.PayStatus),
        PayStatus = payment.PayStatus,
        RefundProcessStatus = payment.RefundProcessStatus,
        RefundStatusDisplayName = payment.RefundProcessStatus is null ? null : DisplayNameService.GetRefundStatusName(payment.RefundProcessStatus)
    };

    private static PaymentSummaryViewModel MapSummary(PaymentRecord payment, string taskTitle) => new()
    {
        PaymentId = payment.PaymentId,
        TaskId = payment.TaskId,
        RecordId = payment.RecordId,
        TaskTitle = taskTitle,
        OrderAmount = payment.OrderAmount,
        PayAmount = payment.PayAmount,
        PayMethodDisplayName = DisplayNameService.GetPayMethodName(payment.PayMethod),
        PayStatusDisplayName = DisplayNameService.GetPayStatusName(payment.PayStatus),
        PayStatus = payment.PayStatus
    };

    private static (string ApplicationReason, string? ReviewReason) SplitRefundReasons(string? combinedReason)
    {
        string value = combinedReason ?? string.Empty;
        int markerIndex = value.LastIndexOf(ReviewReasonMarker, StringComparison.Ordinal);
        return markerIndex < 0
            ? (value, null)
            : (value[..markerIndex], value[(markerIndex + ReviewReasonMarker.Length)..]);
    }
}

public sealed record PaymentOperationResult(bool Success, string ErrorMessage, int PaymentId);
