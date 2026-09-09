using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class RunnerService(IRunnerRepository runnerRepository) : IRunnerService
{
    public async Task<RunnerIndexViewModel> GetIndexAsync(
        bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var allRunnersTask = runnerRepository.GetAllAsync(cancellationToken: cancellationToken);
        var visibleRunnersTask = pendingOnly
            ? runnerRepository.GetAllAsync(pendingOnly: true, cancellationToken)
            : allRunnersTask;

        await Task.WhenAll(allRunnersTask, visibleRunnersTask);
        var allRunners = allRunnersTask.Result;
        var visibleRunners = visibleRunnersTask.Result;

        return new RunnerIndexViewModel
        {
            Runners = visibleRunners.Select(ToListItem).ToList(),
            PendingOnly = pendingOnly,
            PendingCount = allRunners.Count(runner => runner.AuditStatus == "PENDING"),
            ApprovedCount = allRunners.Count(runner => runner.AuditStatus == "APPROVED"),
            AvailableCount = allRunners.Count(runner => runner.AuditStatus == "APPROVED" && runner.WorkStatus == "FREE")
        };
    }

    public async Task<RunnerApplicationPageViewModel> GetApplicationAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var runner = await runnerRepository.GetByUserIdAsync(userId, cancellationToken);
        if (runner is null)
        {
            return new RunnerApplicationPageViewModel
            {
                CanSubmit = await runnerRepository.CanApplyAsync(userId, cancellationToken)
            };
        }

        return new RunnerApplicationPageViewModel
        {
            HasApplication = true,
            CanSubmit = runner.AuditStatus == "REJECTED" && runner.AccountStatus == "NORMAL" && runner.UserRole != "ADMIN",
            IsResubmission = runner.AuditStatus == "REJECTED",
            AuditStatus = runner.AuditStatus,
            AuditStatusDisplayName = DisplayNameService.GetRunnerAuditStatusName(runner.AuditStatus),
            WorkStatus = runner.WorkStatus,
            WorkStatusDisplayName = DisplayNameService.GetRunnerWorkStatusName(runner.WorkStatus),
            Form = new RunnerApplicationFormViewModel
            {
                RealName = runner.RealName,
                IdentityInfo = runner.IdentityInfo
            }
        };
    }

    public async Task<RunnerApplicationResult> SubmitApplicationAsync(
        int userId,
        RunnerApplicationFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var realName = model.RealName.Trim();
        var identityInfo = model.IdentityInfo.Trim();
        var existing = await runnerRepository.GetByUserIdAsync(userId, cancellationToken);

        if (existing is null)
        {
            if (await runnerRepository.InsertApplicationAsync(
                    userId,
                    realName,
                    identityInfo,
                    cancellationToken))
            {
                return RunnerApplicationResult.Success;
            }

            return await runnerRepository.GetByUserIdAsync(userId, cancellationToken) is null
                ? RunnerApplicationResult.AccountUnavailable
                : RunnerApplicationResult.AlreadyExists;
        }

        if (existing.AuditStatus != "REJECTED")
        {
            return RunnerApplicationResult.AlreadyExists;
        }

        return await runnerRepository.ResubmitApplicationAsync(
                userId,
                realName,
                identityInfo,
                cancellationToken)
            ? RunnerApplicationResult.Success
            : RunnerApplicationResult.AccountUnavailable;
    }

    public async Task<RunnerReviewResult> ReviewAsync(
        int runnerId,
        string decision,
        CancellationToken cancellationToken = default)
    {
        var writeResult = decision switch
        {
            "APPROVED" => await runnerRepository.ReviewAsync(runnerId, "APPROVED", cancellationToken),
            "REJECTED" => await runnerRepository.ReviewAsync(runnerId, "REJECTED", cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        };

        return writeResult switch
        {
            RunnerReviewWriteResult.Success => RunnerReviewResult.Success,
            RunnerReviewWriteResult.NotFound => RunnerReviewResult.NotFound,
            RunnerReviewWriteResult.AlreadyReviewed => RunnerReviewResult.AlreadyReviewed,
            _ => RunnerReviewResult.AccountUnavailable
        };
    }

    public async Task<RunnerWorkStatusResult> UpdateWorkStatusAsync(
        int runnerId,
        string workStatus,
        CancellationToken cancellationToken = default)
    {
        if (workStatus is not ("FREE" or "OFFLINE"))
        {
            throw new ArgumentOutOfRangeException(nameof(workStatus));
        }

        var writeResult = await runnerRepository.UpdateWorkStatusAsync(
            runnerId,
            workStatus,
            cancellationToken);

        return writeResult switch
        {
            RunnerWorkStatusWriteResult.Success => RunnerWorkStatusResult.Success,
            RunnerWorkStatusWriteResult.NotFound => RunnerWorkStatusResult.NotFound,
            RunnerWorkStatusWriteResult.Busy => RunnerWorkStatusResult.Busy,
            _ => RunnerWorkStatusResult.Unavailable
        };
    }

    private static RunnerListItemViewModel ToListItem(Runner runner)
    {
        return new RunnerListItemViewModel
        {
            RunnerId = runner.RunnerId,
            UserId = runner.UserId,
            Username = runner.Username,
            Phone = runner.Phone,
            RealName = runner.RealName,
            IdentityInfo = runner.IdentityInfo,
            AuditStatus = runner.AuditStatus,
            AuditStatusDisplayName = DisplayNameService.GetRunnerAuditStatusName(runner.AuditStatus),
            WorkStatus = runner.WorkStatus,
            WorkStatusDisplayName = DisplayNameService.GetRunnerWorkStatusName(runner.WorkStatus),
            CreditScore = runner.CreditScore
        };
    }
}
