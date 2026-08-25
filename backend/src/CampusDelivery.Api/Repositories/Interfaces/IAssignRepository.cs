using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IAssignRepository
{
    Task<IReadOnlyList<CampusTask>> GetGrabableTasksAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetGrabableCountAsync(CancellationToken cancellationToken = default);
    Task<Runner?> GetRunnerByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampusTask>> GetActiveTasksByRunnerIdAsync(int runnerId, int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetActiveTaskCountByRunnerIdAsync(int runnerId, CancellationToken cancellationToken = default);
    Task<int> GetOtherActiveTaskCountByRunnerIdAsync(int runnerId, int excludedTaskId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<TaskDetailsRecord?> GetActiveTaskDetailsAsync(int taskId, int runnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampusTask>> GetTasksWaitingForReceiptAsync(int publisherUserId, int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTasksWaitingForReceiptCountAsync(int publisherUserId, CancellationToken cancellationToken = default);
    Task<string?> GetTaskStatusWithLockAsync(int taskId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<Runner?> GetRunnerWithLockAsync(int runnerId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<int?> GetTaskPublisherUserIdAsync(int taskId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> IsReceiptConfirmedAsync(int recordId, int publisherUserId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateTaskStatusAsync(int taskId, string status, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateRunnerWorkStatusAsync(int runnerId, string workStatus, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> InsertAssignRecordAsync(AssignRecord record, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task InsertTaskStatusLogAsync(TaskStatusLog log, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<AssignRecord?> GetLatestAssignRecordAsync(int taskId, CancellationToken cancellationToken = default);
    Task<AssignRecord?> GetLatestAssignRecordWithLockAsync(int taskId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampusTask>> GetWaitingTasksForAdminAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetWaitingTasksForAdminCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Runner>> GetAvailableRunnersForAdminAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetAvailableRunnersForAdminCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskStatusLog>> GetStatusLogsByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task<string> GetServiceTypeNameAsync(int serviceTypeId, CancellationToken cancellationToken = default);
    Task<string> GetNodeNameAsync(int nodeId, CancellationToken cancellationToken = default);
    Task<(string ContactName, string ContactPhone, string AddressDisplay)> GetAddressDetailsAsync(int userId, int addressNo, CancellationToken cancellationToken = default);
    Task<string> GetUsernameByIdAsync(int userId, CancellationToken cancellationToken = default);
}
