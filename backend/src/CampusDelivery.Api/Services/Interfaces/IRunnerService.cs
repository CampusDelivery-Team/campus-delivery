using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IRunnerService
{
    Task<RunnerIndexViewModel> GetIndexAsync(bool pendingOnly = false, CancellationToken cancellationToken = default);
    Task<RunnerApplicationPageViewModel> GetApplicationAsync(int userId, CancellationToken cancellationToken = default);
    Task<RunnerApplicationResult> SubmitApplicationAsync(int userId, RunnerApplicationFormViewModel model, CancellationToken cancellationToken = default);
    Task<RunnerReviewResult> ReviewAsync(int runnerId, string decision, CancellationToken cancellationToken = default);
    Task<RunnerWorkStatusResult> UpdateWorkStatusAsync(int runnerId, string workStatus, CancellationToken cancellationToken = default);
}
