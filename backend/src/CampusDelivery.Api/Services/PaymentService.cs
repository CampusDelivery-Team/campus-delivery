using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class PaymentService
{
    private readonly TaskRepository _taskRepository;
    private readonly PaymentRepository _paymentRepository;
    private readonly OracleConnectionFactory _connectionFactory;

    public PaymentService(
        TaskRepository taskRepository,
        PaymentRepository paymentRepository,
        OracleConnectionFactory connectionFactory)
    {
        _taskRepository = taskRepository;
        _paymentRepository = paymentRepository;
        _connectionFactory = connectionFactory;
    }

    public async Task<PaymentConfirmViewModel?> BuildConfirmModelAsync(
        int taskId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        TaskDetailsRecord? details = await _taskRepository.GetDetailsAsync(taskId, currentUserId, false, cancellationToken);
        if (details == null || !details.RecordId.HasValue)
        {
            return null;
        }

        IReadOnlyList<TaskStatusLog> logs = await _taskRepository.GetStatusLogsByTaskIdAsync(taskId, cancellationToken);
        bool receiptConfirmed = logs.Any(log => IsReceiptConfirmationLog(log, currentUserId));
        PaymentRecord? payment = await _paymentRepository.GetByTaskIdAsync(taskId, cancellationToken);

        return new PaymentConfirmViewModel
        {
            TaskId = details.Task.TaskId,
            RecordId = details.RecordId.Value,
            TaskTitle = details.Task.TaskTitle,
            TaskAmount = details.Task.TaskPrice,
            TaskStatusDisplayName = DisplayNameService.GetTaskStatusName(details.Task.TaskStatus),
            ReceiptConfirmed = receiptConfirmed,
            CanSubmitPayment = details.Task.TaskStatus == "WAIT_CONFIRM"
                && receiptConfirmed
                && payment?.PayStatus != "PAID"
                && payment?.PayStatus != "REFUNDED"
        };
    }

    public async Task<PaymentOperationResult> SubmitPaymentAsync(
        int taskId,
        int currentUserId,
        string payMethod,
        string? thirdTradeNo,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidPayMethod(payMethod))
        {
            return new PaymentOperationResult(false, "支付方式无效。", 0);
        }

        TaskDetailsRecord? details = await _taskRepository.GetDetailsAsync(taskId, currentUserId, false, cancellationToken);
        if (details == null || !details.RecordId.HasValue)
        {
            return new PaymentOperationResult(false, "任务不存在或无权支付。", 0);
        }

        await using OracleConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleTransaction transaction = (OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            string? taskStatus = await _taskRepository.GetTaskStatusWithLockAsync(taskId, connection, transaction, cancellationToken);
            if (taskStatus != "WAIT_CONFIRM")
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentOperationResult(false, "只有已确认收货的任务才能发起支付。", 0);
            }

            int? taskPublisherUserId = await _taskRepository.GetTaskPublisherUserIdAsync(taskId, connection, transaction, cancellationToken);
            if (taskPublisherUserId != currentUserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentOperationResult(false, "任务不存在或无权支付。", 0);
            }

            AssignRecord? assignRecord = await _taskRepository.GetLatestAssignRecordWithConnectionAsync(taskId, connection, transaction, cancellationToken);
            if (assignRecord == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentOperationResult(false, "只有已产生接派记录的任务才允许收货后支付。", 0);
            }

            bool receiptConfirmed = await _taskRepository.IsReceiptConfirmedAsync(
                assignRecord.RecordId,
                currentUserId,
                connection,
                transaction,
                cancellationToken);
            if (!receiptConfirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentOperationResult(false, "请先确认收货后再完成支付。", 0);
            }

            PaymentRecord? existingPayment = await _paymentRepository.GetByTaskIdWithLockAsync(taskId, connection, transaction, cancellationToken);
            if (existingPayment != null)
            {
                if (existingPayment.PayStatus == "PAID")
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new PaymentOperationResult(false, "该任务已完成支付，不能重复确认付款。", existingPayment.PaymentId);
                }

                if (existingPayment.PayStatus == "REFUNDED")
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new PaymentOperationResult(false, "已退款任务不能重复确认付款。", existingPayment.PaymentId);
                }
            }

            Runner? runner = await _taskRepository.GetRunnerWithLockAsync(assignRecord.RunnerId, connection, transaction, cancellationToken);
            if (runner == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentOperationResult(false, "无法找到当前接单跑腿员。", 0);
            }

            PaymentRecord payment = new PaymentRecord
            {
                TaskId = taskId,
                RecordId = assignRecord.RecordId,
                PublisherUserId = currentUserId,
                OrderAmount = details.Task.TaskPrice,
                PayAmount = details.Task.TaskPrice,
                PayMethod = payMethod,
                ThirdTradeNo = NormalizeText(thirdTradeNo),
                PayStatus = "PAID",
                PaidAt = DateTime.Now
            };

            int paymentId;
            if (existingPayment == null)
            {
                paymentId = await _paymentRepository.InsertAsync(payment, connection, transaction, cancellationToken);
            }
            else
            {
                paymentId = existingPayment.PaymentId;
                await _paymentRepository.UpdatePaymentAsync(paymentId, payment, connection, transaction, cancellationToken);
            }

            await _taskRepository.UpdateTaskStatusAsync(taskId, "FINISHED", connection, transaction, cancellationToken);
            await _taskRepository.UpdateRunnerWorkStatusAsync(assignRecord.RunnerId, "FREE", connection, transaction, cancellationToken);
            await _taskRepository.InsertTaskStatusLogAsync(new TaskStatusLog
            {
                RecordId = assignRecord.RecordId,
                StatusBefore = "WAIT_CONFIRM",
                StatusAfter = "FINISHED",
                OperatorUserId = currentUserId
            }, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new PaymentOperationResult(true, string.Empty, paymentId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentStatusQueryViewModel> GetMyPaymentStatusAsync(
        int currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        int totalCount = await _paymentRepository.GetPaymentsCountByPublisherUserIdAsync(currentUserId, cancellationToken);
        page = ClampPage(page, totalCount, pageSize);
        int offset = (page - 1) * pageSize;
        IReadOnlyList<PaymentListRecord> payments = await _paymentRepository.GetPaymentsByPublisherUserIdAsync(
            currentUserId,
            offset,
            pageSize,
            cancellationToken);

        return new PaymentStatusQueryViewModel
        {
            QueryText = "我的支付记录",
            Payments = payments.Select(MapSummary).ToList(),
            Payment = null,
            PageNumber = page,
            TotalPages = GetTotalPages(totalCount, pageSize),
            TotalCount = totalCount,
            PageSize = pageSize
        };
    }

    public async Task<PaymentStatusQueryViewModel> QueryPaymentAsync(
        int currentUserId,
        int? taskId,
        int? paymentId,
        CancellationToken cancellationToken = default)
    {
        PaymentSummaryViewModel? paymentSummary = null;
        string? queryText = null;

        if (paymentId.HasValue)
        {
            PaymentRecord? payment = await _paymentRepository.GetByIdAsync(paymentId.Value, cancellationToken);
            if (payment != null)
            {
                TaskDetailsRecord? details = await _taskRepository.GetDetailsAsync(payment.TaskId, currentUserId, false, cancellationToken);
                if (details != null)
                {
                    paymentSummary = MapSummary(payment, details.Task.TaskTitle);
                    queryText = $"支付编号 #{payment.PaymentId}";
                }
            }
        }
        else if (taskId.HasValue)
        {
            TaskDetailsRecord? details = await _taskRepository.GetDetailsAsync(taskId.Value, currentUserId, false, cancellationToken);
            if (details != null)
            {
                PaymentRecord? payment = await _paymentRepository.GetByTaskIdAsync(taskId.Value, cancellationToken);
                if (payment != null)
                {
                    paymentSummary = MapSummary(payment, details.Task.TaskTitle);
                    queryText = $"任务编号 #{taskId.Value}";
                }
            }
        }

        return new PaymentStatusQueryViewModel
        {
            TaskId = taskId,
            PaymentId = paymentId,
            QueryText = queryText,
            Payment = paymentSummary,
            Payments = Array.Empty<PaymentSummaryViewModel>()
        };
    }

    private static bool IsValidPayMethod(string value)
    {
        return value is "WECHAT" or "ALIPAY" or "CASH";
    }

    private static PaymentSummaryViewModel MapSummary(PaymentListRecord record)
    {
        return new PaymentSummaryViewModel
        {
            PaymentId = record.PaymentId,
            TaskId = record.TaskId,
            RecordId = record.RecordId,
            TaskTitle = record.TaskTitle,
            OrderAmount = record.OrderAmount,
            PayAmount = record.PayAmount,
            PayMethodDisplayName = DisplayNameService.GetPayMethodName(record.PayMethod),
            PayStatusDisplayName = DisplayNameService.GetPayStatusName(record.PayStatus),
            ThirdTradeNo = record.ThirdTradeNo,
            PaidAt = record.PaidAt,
            CreatedAt = record.CreatedAt
        };
    }

    private static PaymentSummaryViewModel MapSummary(PaymentRecord record, string taskTitle)
    {
        return new PaymentSummaryViewModel
        {
            PaymentId = record.PaymentId,
            TaskId = record.TaskId,
            RecordId = record.RecordId,
            TaskTitle = taskTitle,
            OrderAmount = record.OrderAmount,
            PayAmount = record.PayAmount,
            PayMethodDisplayName = DisplayNameService.GetPayMethodName(record.PayMethod),
            PayStatusDisplayName = DisplayNameService.GetPayStatusName(record.PayStatus),
            ThirdTradeNo = record.ThirdTradeNo,
            PaidAt = record.PaidAt,
            CreatedAt = record.CreatedAt
        };
    }

    private static int GetTotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize);
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

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsReceiptConfirmationLog(TaskStatusLog log, int publisherUserId)
    {
        return log.StatusBefore == "WAIT_CONFIRM"
            && log.StatusAfter == "WAIT_CONFIRM"
            && log.OperatorUserId == publisherUserId;
    }
}

public sealed record PaymentOperationResult(bool Success, string ErrorMessage, int PaymentId);
