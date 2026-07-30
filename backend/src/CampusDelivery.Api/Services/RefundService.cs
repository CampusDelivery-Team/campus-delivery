using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class RefundService
{
    private readonly TaskRepository _taskRepository;
    private readonly PaymentRepository _paymentRepository;
    private readonly RefundRepository _refundRepository;
    private readonly OracleConnectionFactory _connectionFactory;

    public RefundService(
        TaskRepository taskRepository,
        PaymentRepository paymentRepository,
        RefundRepository refundRepository,
        OracleConnectionFactory connectionFactory)
    {
        _taskRepository = taskRepository;
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _connectionFactory = connectionFactory;
    }

    public async Task<RefundCreateViewModel?> BuildCreateModelAsync(
        int taskId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        TaskDetailsRecord? details = await _taskRepository.GetDetailsAsync(taskId, currentUserId, false, cancellationToken);
        if (details == null)
        {
            return null;
        }

        PaymentRecord? payment = await _paymentRepository.GetByTaskIdAsync(taskId, cancellationToken);
        if (payment == null || payment.PayStatus != "PAID")
        {
            return null;
        }

        RefundRecord? refund = await _refundRepository.GetByPaymentIdAsync(payment.PaymentId, cancellationToken);
        if (refund != null && refund.RefundStatus is "PENDING" or "APPROVED")
        {
            return null;
        }

        return new RefundCreateViewModel
        {
            PaymentId = payment.PaymentId,
            TaskId = taskId,
            RecordId = details.RecordId ?? payment.RecordId,
            TaskTitle = details.Task.TaskTitle,
            RefundAmount = payment.PayAmount,
            PayStatusDisplayName = DisplayNameService.GetPayStatusName(payment.PayStatus)
        };
    }

    public async Task<RefundOperationResult> SubmitAsync(
        RefundCreateViewModel model,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        TaskDetailsRecord? details = await _taskRepository.GetDetailsAsync(model.TaskId, currentUserId, false, cancellationToken);
        if (details == null)
        {
            return new RefundOperationResult(false, "任务不存在或无权申请退款。", 0);
        }

        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            PaymentRecord? payment = await _paymentRepository.GetByIdWithLockAsync(model.PaymentId, connection, transaction, cancellationToken);
            if (payment == null || payment.TaskId != model.TaskId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "退款登记必须关联有效支付记录。", 0);
            }

            if (payment.PublisherUserId != currentUserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "无权对该支付记录申请退款。", 0);
            }

            if (payment.PayStatus != "PAID")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "只有已支付记录才能申请退款。", 0);
            }

            RefundRecord? existingRefund = await _refundRepository.GetByPaymentIdWithLockAsync(payment.PaymentId, connection, transaction, cancellationToken);
            if (existingRefund != null && existingRefund.RefundStatus is "PENDING" or "APPROVED")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "该支付记录已存在退款申请。", existingRefund.RefundId);
            }

            string? taskStatus = await _taskRepository.GetTaskStatusWithLockAsync(model.TaskId, connection, transaction, cancellationToken);
            if (taskStatus != "FINISHED")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "只有已完成任务才能登记退款。", 0);
            }

            AssignRecord? assignRecord = await _taskRepository.GetLatestAssignRecordWithConnectionAsync(model.TaskId, connection, transaction, cancellationToken);
            if (assignRecord == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "退款登记必须关联接派记录。", 0);
            }

            await _taskRepository.UpdateTaskStatusAsync(model.TaskId, "REFUNDING", connection, transaction, cancellationToken);
            await _taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = "FINISHED",
                StatusAfter = "REFUNDING",
                OperatorUserId = currentUserId
            }, connection, transaction, cancellationToken);

            RefundRecord refund = new RefundRecord
            {
                PaymentId = payment.PaymentId,
                TaskId = model.TaskId,
                RecordId = assignRecord.RecordId,
                RequestUserId = currentUserId,
                RefundAmount = payment.PayAmount,
                RefundStatus = "PENDING",
                RefundReason = model.RefundReason.Trim()
            };

            int refundId = await _refundRepository.InsertAsync(refund, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RefundOperationResult(true, string.Empty, refundId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RefundAdminListViewModel> GetAdminListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        int totalCount = await _refundRepository.GetRefundsCountAsync(cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;
        IReadOnlyList<RefundListRecord> refunds = await _refundRepository.GetRefundsAsync(offset, pageSize, cancellationToken);

        RefundAdminListViewModel model = new RefundAdminListViewModel
        {
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = GetTotalPages(totalCount, pageSize)
        };

        foreach (RefundListRecord refund in refunds)
        {
            model.Refunds.Add(new RefundSummaryViewModel
            {
                RefundId = refund.RefundId,
                PaymentId = refund.PaymentId,
                TaskId = refund.TaskId,
                TaskTitle = refund.TaskTitle,
                RefundAmount = refund.RefundAmount,
                RefundStatusDisplayName = DisplayNameService.GetRefundStatusName(refund.RefundStatus),
                RefundReason = refund.RefundReason,
                ReviewReason = refund.ReviewReason,
                ReviewedByName = refund.ReviewedByName,
                CreatedAt = refund.CreatedAt,
                ReviewedAt = refund.ReviewedAt
            });
        }

        return model;
    }

    public async Task<RefundReviewViewModel?> GetReviewModelAsync(int refundId, CancellationToken cancellationToken = default)
    {
        RefundRecord? refund = await _refundRepository.GetByIdAsync(refundId, cancellationToken);
        if (refund == null)
        {
            return null;
        }

        TaskDetailsRecord? details = await _taskRepository.GetDetailsAsync(refund.TaskId, refund.RequestUserId, true, cancellationToken);
        if (details == null)
        {
            return null;
        }

        return new RefundReviewViewModel
        {
            RefundId = refund.RefundId,
            PaymentId = refund.PaymentId,
            TaskId = refund.TaskId,
            TaskTitle = details.Task.TaskTitle,
            RefundAmount = refund.RefundAmount,
            RefundReason = refund.RefundReason ?? string.Empty,
            Decision = refund.RefundStatus == "REJECTED" ? "REJECTED" : "APPROVED",
            ReviewReason = refund.ReviewReason ?? string.Empty
        };
    }

    public async Task<RefundOperationResult> ReviewAsync(
        int refundId,
        string decision,
        string reviewReason,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (decision is not "APPROVED" and not "REJECTED")
        {
            return new RefundOperationResult(false, "审核结果无效。", 0);
        }

        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            RefundRecord? refund = await _refundRepository.GetByIdWithLockAsync(refundId, connection, transaction, cancellationToken);
            if (refund == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "退款记录不存在。", 0);
            }

            if (refund.RefundStatus != "PENDING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "只有待审核退款可以处理。", refund.RefundId);
            }

            PaymentRecord? payment = await _paymentRepository.GetByIdWithLockAsync(refund.PaymentId, connection, transaction, cancellationToken);
            if (payment == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "关联支付记录不存在。", refund.RefundId);
            }

            string? taskStatus = await _taskRepository.GetTaskStatusWithLockAsync(refund.TaskId, connection, transaction, cancellationToken);
            if (taskStatus != "REFUNDING")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "当前任务不处于退款处理中。", refund.RefundId);
            }

            AssignRecord? assignRecord = await _taskRepository.GetLatestAssignRecordWithConnectionAsync(refund.TaskId, connection, transaction, cancellationToken);
            if (assignRecord == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefundOperationResult(false, "退款审核必须关联接派记录。", refund.RefundId);
            }

            await _refundRepository.UpdateRefundStatusAsync(
                refund.RefundId,
                decision,
                reviewReason.Trim(),
                adminUserId,
                DateTime.Now,
                connection,
                transaction,
                cancellationToken);

            if (decision == "APPROVED")
            {
                await _paymentRepository.UpdatePaymentStatusAsync(
                    payment.PaymentId,
                    "REFUNDED",
                    payment.ThirdTradeNo,
                    payment.PaidAt,
                    connection,
                    transaction,
                    cancellationToken);
            }

            await _taskRepository.UpdateTaskStatusAsync(refund.TaskId, "FINISHED", connection, transaction, cancellationToken);
            await _taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = "REFUNDING",
                StatusAfter = "FINISHED",
                OperatorUserId = adminUserId
            }, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new RefundOperationResult(true, string.Empty, refund.RefundId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 20 ? pageSize : 10;
        return (page, pageSize);
    }

    private static int ClampPage(int page, int totalCount, int pageSize)
    {
        int totalPages = GetTotalPages(totalCount, pageSize);
        return Math.Min(page, totalPages);
    }

    private static int GetTotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize);
    }
}

public sealed record RefundOperationResult(bool Success, string ErrorMessage, int RefundId);
