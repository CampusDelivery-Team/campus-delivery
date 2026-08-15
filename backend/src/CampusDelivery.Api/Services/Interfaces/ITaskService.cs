using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface ITaskService
{
    Task<TaskCreateViewModel> BuildCreateModelAsync(int userId, CancellationToken cancellationToken = default);
    Task PopulateCreateOptionsAsync(TaskCreateViewModel model, int userId, CancellationToken cancellationToken = default);
    Task<TaskOperationResult> CreateAsync(int userId, TaskCreateViewModel model, CancellationToken cancellationToken = default);
    Task<TaskIndexViewModel> GetIndexAsync(int currentUserId, bool includeAll, CancellationToken cancellationToken = default);
    Task<string> CancelAsync(int taskId, int currentUserId, CancellationToken cancellationToken = default);
    Task<TaskDetailsViewModel?> GetDetailsAsync(int taskId, int currentUserId, bool includeAll, CancellationToken cancellationToken = default);
}
