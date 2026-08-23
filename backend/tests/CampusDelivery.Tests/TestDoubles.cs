using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories.Interfaces;

namespace CampusDelivery.Tests;

internal sealed class FakeRepositoryTransactionManager : IRepositoryTransactionManager
{
    private readonly SemaphoreSlim _taskRowLock = new(1, 1);
    private readonly object _transactionsLock = new();

    public List<FakeRepositoryTransaction> Transactions { get; } = [];

    public Task<IRepositoryTransaction> BeginAsync(CancellationToken cancellationToken = default)
    {
        var transaction = new FakeRepositoryTransaction(_taskRowLock);
        lock (_transactionsLock)
        {
            Transactions.Add(transaction);
        }

        return Task.FromResult<IRepositoryTransaction>(transaction);
    }
}

internal sealed class FakeRepositoryTransaction(SemaphoreSlim taskRowLock) : IRepositoryTransaction
{
    private bool _completed;
    private bool _lockHeld;

    public bool WasCommitted { get; private set; }

    public bool WasRolledBack { get; private set; }

    public async Task AcquireTaskRowAsync(CancellationToken cancellationToken)
    {
        if (_lockHeld)
        {
            return;
        }

        await taskRowLock.WaitAsync(cancellationToken);
        _lockHeld = true;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!_completed)
        {
            WasCommitted = true;
            Complete();
        }

        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (!_completed)
        {
            WasRolledBack = true;
            Complete();
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            WasRolledBack = true;
            Complete();
        }

        return ValueTask.CompletedTask;
    }

    private void Complete()
    {
        _completed = true;
        if (_lockHeld)
        {
            taskRowLock.Release();
            _lockHeld = false;
        }
    }
}

internal sealed class FakeAssignRepository : IAssignRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<int, Runner> _runnersByUserId = [];

    public int TaskId { get; set; } = 101;

    public int PublisherUserId { get; set; } = 501;

    public string? TaskStatus { get; set; } = "WAITING";

    public AssignRecord? LatestAssignRecord { get; set; }

    public bool ReceiptConfirmed { get; set; }

    public List<CampusTask> ActiveTasks { get; } = [];

    public TaskDetailsRecord? ActiveTaskDetails { get; set; }

    public List<AssignRecord> InsertedAssignRecords { get; } = [];

    public List<TaskStatusLog> InsertedStatusLogs { get; } = [];

    public void AddRunner(int userId, int runnerId, string workStatus = "FREE")
    {
        lock (_sync)
        {
            _runnersByUserId[userId] = new Runner
            {
                UserId = userId,
                RunnerId = runnerId,
                AuditStatus = "APPROVED",
                WorkStatus = workStatus
            };
        }
    }

    public Runner? GetRunner(int runnerId)
    {
        lock (_sync)
        {
            return _runnersByUserId.Values.SingleOrDefault(runner => runner.RunnerId == runnerId);
        }
    }

    public Task<Runner?> GetRunnerByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _runnersByUserId.TryGetValue(userId, out Runner? runner);
            return Task.FromResult(runner);
        }
    }

    public async Task<string?> GetTaskStatusWithLockAsync(
        int taskId,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await GetTransaction(transaction).AcquireTaskRowAsync(cancellationToken);
        lock (_sync)
        {
            return taskId == TaskId ? TaskStatus : null;
        }
    }

    public Task<Runner?> GetRunnerWithLockAsync(
        int runnerId,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetRunner(runnerId));
    }

    public Task<int?> GetTaskPublisherUserIdAsync(
        int taskId,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<int?>(taskId == TaskId ? PublisherUserId : null);
    }

    public Task<bool> IsReceiptConfirmedAsync(
        int recordId,
        int publisherUserId,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(
                ReceiptConfirmed
                && LatestAssignRecord?.RecordId == recordId
                && publisherUserId == PublisherUserId);
        }
    }

    public Task UpdateTaskStatusAsync(
        int taskId,
        string status,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (taskId == TaskId)
            {
                TaskStatus = status;
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateRunnerWorkStatusAsync(
        int runnerId,
        string workStatus,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            Runner? runner = _runnersByUserId.Values.SingleOrDefault(item => item.RunnerId == runnerId);
            if (runner is not null)
            {
                runner.WorkStatus = workStatus;
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> InsertAssignRecordAsync(
        AssignRecord record,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            record.RecordId = 900 + InsertedAssignRecords.Count;
            record.AssignedAt = DateTime.Now;
            InsertedAssignRecords.Add(record);
            LatestAssignRecord = record;
            return Task.FromResult(record.RecordId);
        }
    }

    public Task InsertTaskStatusLogAsync(
        TaskStatusLog log,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            InsertedStatusLogs.Add(log);
            if (log.StatusBefore == "WAIT_CONFIRM"
                && log.StatusAfter == "WAIT_CONFIRM"
                && log.OperatorUserId == PublisherUserId)
            {
                ReceiptConfirmed = true;
            }
        }

        return Task.CompletedTask;
    }

    public Task<AssignRecord?> GetLatestAssignRecordAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(taskId == TaskId ? LatestAssignRecord : null);
        }
    }

    public Task<AssignRecord?> GetLatestAssignRecordWithLockAsync(
        int taskId,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(taskId == TaskId ? LatestAssignRecord : null);
        }
    }

    public Task<IReadOnlyList<TaskStatusLog>> GetStatusLogsByTaskIdAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<TaskStatusLog>>(
                taskId == TaskId ? InsertedStatusLogs.ToList() : []);
        }
    }

    public Task<IReadOnlyList<CampusTask>> GetGrabableTasksAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CampusTask>>([]);

    public Task<int> GetGrabableCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<IReadOnlyList<CampusTask>> GetActiveTasksByRunnerIdAsync(
        int runnerId,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CampusTask>>(ActiveTasks.Skip(offset).Take(pageSize).ToList());

    public Task<int> GetActiveTaskCountByRunnerIdAsync(
        int runnerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveTasks.Count);

    public Task<TaskDetailsRecord?> GetActiveTaskDetailsAsync(
        int taskId,
        int runnerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(taskId == TaskId && GetRunner(runnerId) is not null ? ActiveTaskDetails : null);

    public Task<IReadOnlyList<CampusTask>> GetTasksWaitingForReceiptAsync(
        int publisherUserId,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CampusTask>>([]);

    public Task<int> GetTasksWaitingForReceiptCountAsync(
        int publisherUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<IReadOnlyList<CampusTask>> GetWaitingTasksForAdminAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CampusTask>>([]);

    public Task<int> GetWaitingTasksForAdminCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<IReadOnlyList<Runner>> GetFreeRunnersForAdminAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Runner>>([]);

    public Task<int> GetFreeRunnersForAdminCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<string> GetServiceTypeNameAsync(
        int serviceTypeId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult("测试服务");

    public Task<string> GetNodeNameAsync(
        int nodeId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult("测试节点");

    public Task<(string ContactName, string ContactPhone, string AddressDisplay)> GetAddressDetailsAsync(
        int userId,
        int addressNo,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(("测试用户", "13800000000", "测试校区 1 号楼"));

    public Task<string> GetUsernameByIdAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"user-{userId}");

    private static FakeRepositoryTransaction GetTransaction(IRepositoryTransaction transaction) =>
        transaction as FakeRepositoryTransaction
        ?? throw new ArgumentException("测试事务类型不正确。", nameof(transaction));
}

internal sealed class FakeTaskRepository : ITaskRepository
{
    public TaskCreateWriteResult CreateResult { get; set; } =
        new(TaskCreateResult.Success, 101);

    public TaskCancelResult CancelResult { get; set; } = TaskCancelResult.Success;

    public TaskDetailsRecord? Details { get; set; }

    public TaskPublishRequest? CapturedCreateRequest { get; private set; }

    public Task<TaskCreateWriteResult> CreateAsync(
        TaskPublishRequest request,
        CancellationToken cancellationToken = default)
    {
        CapturedCreateRequest = request;
        return Task.FromResult(CreateResult);
    }

    public Task<IReadOnlyList<TaskRecord>> GetListAsync(
        int currentUserId,
        bool includeAll,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskRecord>>([]);

    public Task<TaskCancelResult> CancelAsync(
        int taskId,
        int currentUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CancelResult);

    public Task<TaskDetailsRecord?> GetDetailsAsync(
        int taskId,
        int currentUserId,
        bool includeAll,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Details);
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    public PaymentRecord? ExistingPayment { get; set; }

    public int InsertCount { get; private set; }

    public Task<PaymentRecord?> GetByTaskIdAsync(
        int taskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ExistingPayment?.TaskId == taskId ? ExistingPayment : null);

    public Task<PaymentRecord?> GetByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ExistingPayment?.PaymentId == paymentId ? ExistingPayment : null);

    public Task<PaymentRecord?> GetByTaskIdWithLockAsync(
        int taskId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default) =>
        GetByTaskIdAsync(taskId, cancellationToken);

    public Task<PaymentRecord?> GetByIdWithLockAsync(
        int paymentId,
        IRepositoryTransaction repositoryTransaction,
        CancellationToken cancellationToken = default) =>
        GetByIdAsync(paymentId, cancellationToken);

    public Task<IReadOnlyList<PaymentListRecord>> GetPaymentsByPublisherUserIdAsync(
        int publisherUserId,
        string? keyword,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentListRecord>>([]);

    public Task<int> GetPaymentsCountByPublisherUserIdAsync(
        int publisherUserId,
        string? keyword,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<int> InsertAsync(
        PaymentRecord record,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        InsertCount++;
        record.PaymentId = 701;
        ExistingPayment = record;
        return Task.FromResult(record.PaymentId);
    }

    public Task UpdatePaymentAsync(
        int paymentId,
        PaymentRecord record,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        record.PaymentId = paymentId;
        ExistingPayment = record;
        return Task.CompletedTask;
    }

    public Task UpdatePaymentStatusAsync(
        int paymentId,
        string payStatus,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (ExistingPayment?.PaymentId == paymentId)
        {
            ExistingPayment.PayStatus = payStatus;
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeRefundRepository : IRefundRepository
{
    public RefundRecord? ActiveRefund { get; set; }

    public Task<RefundRecord?> GetByIdAsync(
        int refundId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<RefundRecord?>(null);

    public Task<RefundRecord?> GetByPaymentIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<RefundRecord?>(null);

    public Task<RefundRecord?> GetActiveByPaymentIdWithLockAsync(
        int paymentId,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveRefund?.PaymentId == paymentId ? ActiveRefund : null);

    public Task<RefundRecord?> GetByIdWithLockAsync(
        int refundId,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<RefundRecord?>(null);

    public Task<IReadOnlyList<RefundListRecord>> GetRefundsAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RefundListRecord>>([]);

    public Task<int> GetRefundsCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<int> InsertAsync(
        RefundRecord record,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(1);

    public Task UpdateReviewAsync(
        int refundId,
        string processStatus,
        decimal approvedAmount,
        string combinedReason,
        IRepositoryTransaction transaction,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class NotUsedAddressRepository : IAddressRepository
{
    public List<UserAddress> GetAddressesByUserId(int userId) => throw NotUsed();
    public UserAddress? GetAddress(int userId, int addressNo) => throw NotUsed();
    public Task<bool> LockUserAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<int> GetNextAddressNoAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> InsertAddressAsync(UserAddress address, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<UserAddress?> GetAddressWithLockAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> UpdateAddressAsync(UserAddress address, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task ClearDefaultAddressesAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> SetDefaultAddressAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> DeleteAddressAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> SetFirstAddressAsDefaultAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => throw NotUsed();

    private static InvalidOperationException NotUsed() =>
        new("该测试不应访问地址仓储。");
}

internal sealed class NotUsedServiceTypeRepository : IServiceTypeRepository
{
    public Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> ExistsByNameAsync(string serviceName, int? excludedServiceTypeId = null, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<ServiceTypeRepositoryWriteResult> InsertAsync(ServiceType serviceType, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<ServiceTypeRepositoryWriteResult> UpdateAsync(ServiceType serviceType, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> UpdateStatusAsync(int serviceTypeId, string typeStatus, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<ServiceTypeDeleteResult> DeleteAsync(int serviceTypeId, CancellationToken cancellationToken = default) => throw NotUsed();

    private static InvalidOperationException NotUsed() =>
        new("该测试不应访问服务类型仓储。");
}

internal sealed class NotUsedNodeRepository : INodeRepository
{
    public Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<Node?> GetByIdAsync(int nodeId, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> ExistsByNameAsync(string nodeName, int? excludedNodeId = null, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task InsertAsync(Node node, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> UpdateAsync(Node node, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<bool> UpdateStatusAsync(int nodeId, string nodeStatus, CancellationToken cancellationToken = default) => throw NotUsed();
    public Task<NodeDeleteResult> DeleteAsync(int nodeId, CancellationToken cancellationToken = default) => throw NotUsed();

    private static InvalidOperationException NotUsed() =>
        new("该测试不应访问节点仓储。");
}
