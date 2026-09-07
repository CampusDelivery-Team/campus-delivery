using System.Text;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class RefundService(
    ITaskRepository taskRepository,
    IAssignRepository assignRepository,
    IPaymentRepository paymentRepository,
    IRefundRepository refundRepository,
    IRepositoryTransactionManager transactionManager) : IRefundService
{
    private const string ReviewReasonMarker = "\n审核意见：";

    public async Task<RefundCreateViewModel?> BuildCreateModelAsync(int taskId, int currentUserId, CancellationToken cancellationToken = default)
    {
        TaskDetailsRecord? details = await taskRepository.GetDetailsAsync(taskId, currentUserId, false, cancellationToken);
        PaymentRecord? payment = await paymentRepository.GetByTaskIdAsync(taskId, cancellationToken);
        if (details is null || payment is null || payment.PayStatus != PaymentStatusCodes.Paid)
        {
            return null;
        }

        RefundRecord? latest = await refundRepository.GetByPaymentIdAsync(payment.PaymentId, cancellationToken);
        if (latest != null && RefundStatusCodes.IsActive(latest.ProcessStatus))
        {
            return null;
        }

        return new()
        {
            PaymentId = payment.PaymentId,
            TaskId = taskId,
            RecordId = payment.RecordId,
            TaskTitle = details.Task.TaskTitle,
            RefundAmount = payment.PayAmount,
            PayStatusDisplayName = DisplayNameService.GetPayStatusName(payment.PayStatus)
        };
    }

    public async Task<RefundOperationResult> SubmitAsync(RefundCreateViewModel model, int currentUserId, CancellationToken cancellationToken = default)
    {
        string refundReason = model.RefundReason?.Trim() ?? string.Empty;
        if (refundReason.Length == 0)
        {
            return new(false, "请填写退款原因。", 0);
        }
        if (refundReason.Length > 180 || Encoding.UTF8.GetByteCount(refundReason) > 180)
        {
            return new(false, "退款原因内容过长（中文建议不超过 60 字）。", 0);
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            string? taskStatus = await assignRepository.GetTaskStatusWithLockAsync(
                model.TaskId,
                transaction,
                cancellationToken);
            PaymentRecord? payment = await paymentRepository.GetByIdWithLockAsync(model.PaymentId, transaction, cancellationToken);
            if (payment is null || payment.TaskId != model.TaskId || payment.PublisherUserId != currentUserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "退款申请必须关联当前用户的有效支付记录。", 0);
            }
            if (payment.PayStatus != PaymentStatusCodes.Paid)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "只有已支付记录才能申请退款。", 0);
            }
            if (await refundRepository.GetActiveByPaymentIdWithLockAsync(payment.PaymentId, transaction, cancellationToken) is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "该支付记录已有待处理或已通过的退款申请。", 0);
            }

            AssignRecord? assignRecord = await assignRepository.GetLatestAssignRecordWithLockAsync(payment.TaskId, transaction, cancellationToken);
            if (taskStatus != TaskStatusCodes.Finished || assignRecord is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "只有已完成且存在接单记录的任务才能申请退款。", 0);
            }

            RefundRecord refund = new()
            {
                PaymentId = payment.PaymentId,
                RefundAmount = payment.PayAmount,
                RefundReason = refundReason,
                ProcessStatus = RefundStatusCodes.Apply
            };
            int refundId = await refundRepository.InsertAsync(refund, transaction, cancellationToken);
            await assignRepository.UpdateTaskStatusAsync(payment.TaskId, TaskStatusCodes.Refunding, transaction, cancellationToken);
            await assignRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = TaskStatusCodes.Finished,
                StatusAfter = TaskStatusCodes.Refunding,
                OperatorUserId = currentUserId
            }, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, string.Empty, refundId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RefundAdminListViewModel> GetAdminListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 20 ? pageSize : 10;
        int count = await refundRepository.GetRefundsCountAsync(cancellationToken);
        int totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Min(page, totalPages);
        IReadOnlyList<RefundListRecord> refunds = await refundRepository.GetRefundsAsync((page - 1) * pageSize, pageSize, cancellationToken);
        return new()
        {
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = count,
            TotalPages = totalPages,
            Refunds = refunds.Select(refund =>
            {
                (string applicationReason, string? reviewReason) = SplitReasons(refund.RefundReason);
                return new RefundSummaryViewModel
                {
                    RefundId = refund.RefundId,
                    PaymentId = refund.PaymentId,
                    TaskId = refund.TaskId,
                    TaskTitle = refund.TaskTitle,
                    RefundAmount = refund.RefundAmount,
                    ApprovedAmount = refund.ApprovedAmount,
                    RefundStatus = refund.ProcessStatus,
                    RefundStatusDisplayName = DisplayNameService.GetRefundStatusName(refund.ProcessStatus),
                    RefundReason = applicationReason,
                    ReviewReason = reviewReason
                };
            }).ToList()
        };
    }

    public async Task<RefundReviewViewModel?> GetReviewModelAsync(int refundId, CancellationToken cancellationToken = default)
    {
        RefundRecord? refund = await refundRepository.GetByIdAsync(refundId, cancellationToken);
        if (refund?.ProcessStatus != RefundStatusCodes.Apply)
        {
            return null;
        }

        PaymentRecord? payment = await paymentRepository.GetByIdAsync(refund.PaymentId, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        TaskDetailsRecord? details = await taskRepository.GetDetailsAsync(payment.TaskId, payment.PublisherUserId, true, cancellationToken);
        return details is null ? null : new RefundReviewViewModel
        {
            RefundId = refund.RefundId,
            PaymentId = refund.PaymentId,
            TaskId = payment.TaskId,
            TaskTitle = details.Task.TaskTitle,
            RefundAmount = refund.RefundAmount,
            RefundReason = SplitReasons(refund.RefundReason).ApplicationReason
        };
    }

    public async Task<RefundOperationResult> ReviewAsync(
        int refundId,
        string decision,
        string reviewReason,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (decision is not (RefundStatusCodes.Approved or RefundStatusCodes.Rejected))
        {
            return new(false, "审核结果无效。", 0);
        }

        reviewReason = reviewReason?.Trim() ?? string.Empty;
        if (reviewReason.Length == 0)
        {
            return new(false, "请填写审核理由。", 0);
        }
        if (reviewReason.Length > 100 || Encoding.UTF8.GetByteCount(reviewReason) > 100)
        {
            return new(false, "审核理由内容过长（中文建议不超过 33 字）。", 0);
        }

        RefundRecord? refundReference = await refundRepository.GetByIdAsync(refundId, cancellationToken);
        PaymentRecord? paymentReference = refundReference is null
            ? null
            : await paymentRepository.GetByIdAsync(refundReference.PaymentId, cancellationToken);
        if (refundReference is null || paymentReference is null)
        {
            return new(false, "退款关联的任务或支付记录不存在。", refundId);
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            string? taskStatus = await assignRepository.GetTaskStatusWithLockAsync(
                paymentReference.TaskId,
                transaction,
                cancellationToken);
            PaymentRecord? payment = await paymentRepository.GetByIdWithLockAsync(
                paymentReference.PaymentId,
                transaction,
                cancellationToken);
            RefundRecord? refund = await refundRepository.GetByIdWithLockAsync(
                refundId,
                transaction,
                cancellationToken);
            if (refund?.ProcessStatus != RefundStatusCodes.Apply)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "只有待审核退款可以处理。", refund?.RefundId ?? 0);
            }

            if (payment is null
                || payment.PaymentId != refund.PaymentId
                || payment.TaskId != paymentReference.TaskId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "退款关联的支付记录已变化，请刷新后重试。", refund.RefundId);
            }

            PaymentRecord lockedPayment = payment;
            AssignRecord? assignRecord = await assignRepository.GetLatestAssignRecordWithLockAsync(
                lockedPayment.TaskId,
                transaction,
                cancellationToken);
            if (taskStatus != TaskStatusCodes.Refunding || assignRecord is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "退款关联的任务或支付记录状态不正确。", refund.RefundId);
            }

            string applicationReason = SplitReasons(refund.RefundReason).ApplicationReason;
            string combinedReason = applicationReason + ReviewReasonMarker + reviewReason;
            if (combinedReason.Length > 300 || Encoding.UTF8.GetByteCount(combinedReason) > 300)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(false, "退款原因和审核理由合计不能超过 300 个字符。", refund.RefundId);
            }

            decimal approvedAmount = decision == RefundStatusCodes.Approved ? refund.RefundAmount : 0;
            await refundRepository.UpdateReviewAsync(
                refund.RefundId,
                decision,
                approvedAmount,
                combinedReason,
                transaction,
                cancellationToken);
            if (decision == RefundStatusCodes.Approved)
            {
                await paymentRepository.UpdatePaymentStatusAsync(
                    lockedPayment.PaymentId,
                    PaymentStatusCodes.Refunded,
                    transaction,
                    cancellationToken);
            }

            await assignRepository.UpdateTaskStatusAsync(
                lockedPayment.TaskId,
                TaskStatusCodes.Finished,
                transaction,
                cancellationToken);
            await assignRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = TaskStatusCodes.Refunding,
                StatusAfter = TaskStatusCodes.Finished,
                OperatorUserId = adminUserId
            }, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, string.Empty, refund.RefundId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static (string ApplicationReason, string? ReviewReason) SplitReasons(string? combinedReason)
    {
        string value = combinedReason ?? string.Empty;
        int markerIndex = value.LastIndexOf(ReviewReasonMarker, StringComparison.Ordinal);
        return markerIndex < 0
            ? (value, null)
            : (value[..markerIndex], value[(markerIndex + ReviewReasonMarker.Length)..]);
    }
}
