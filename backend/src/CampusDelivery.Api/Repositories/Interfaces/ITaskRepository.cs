using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<TaskCreateWriteResult> CreateAsync(TaskPublishRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskRecord>> GetListAsync(int currentUserId, bool includeAll, CancellationToken cancellationToken = default);
    Task<TaskCancelResult> CancelAsync(int taskId, int currentUserId, CancellationToken cancellationToken = default);
    Task<TaskDetailsRecord?> GetDetailsAsync(int taskId, int currentUserId, bool includeAll, CancellationToken cancellationToken = default);
}
