using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IRunnerRepository
{
    Task<IReadOnlyList<Runner>> GetAllAsync(bool pendingOnly = false, CancellationToken cancellationToken = default);
    Task<Runner?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> CanApplyAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> InsertApplicationAsync(int userId, string realName, string identityInfo, CancellationToken cancellationToken = default);
    Task<bool> ResubmitApplicationAsync(int userId, string realName, string identityInfo, CancellationToken cancellationToken = default);
    Task<RunnerReviewWriteResult> ReviewAsync(int runnerId, string auditStatus, CancellationToken cancellationToken = default);
    Task<RunnerWorkStatusWriteResult> UpdateWorkStatusAsync(int runnerId, string workStatus, CancellationToken cancellationToken = default);
}
