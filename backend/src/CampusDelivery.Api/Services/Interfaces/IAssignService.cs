using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IAssignService
{
    Task<TaskHallViewModel> GetTaskHallAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MyTasksViewModel?> GetMyTasksAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ReceiptTasksViewModel> GetReceiptTasksAsync(int publisherUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminAssignViewModel> GetAdminConsoleAsync(int taskPage, int runnerPage, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> GrabTaskAsync(int taskId, int userId, CancellationToken cancellationToken = default);
    Task<bool> AssignTaskAsync(int taskId, int runnerId, int adminUserId, CancellationToken cancellationToken = default);
    Task<bool> ReassignTaskAsync(int taskId, int newRunnerId, string? reason, int adminUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int taskId, string targetStatus, int operatorUserId, CancellationToken cancellationToken = default);
    Task<bool> ConfirmReceiptAsync(int taskId, int publisherUserId, CancellationToken cancellationToken = default);
}
