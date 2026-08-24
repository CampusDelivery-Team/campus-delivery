using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class SettlementService(
    ISettlementRepository settlementRepository,
    IRepositoryTransactionManager transactionManager) : ISettlementService
{
    public const decimal PlatformFeeRate = 0.10m;

    public async Task<SettlementIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Settlement> settlements = await settlementRepository.GetRecentSettlementsAsync(cancellationToken);
        SettlementCandidateSummary candidateSummary =
            await settlementRepository.GetSettlementCandidateSummaryAsync(cancellationToken);

        return new SettlementIndexViewModel
        {
            Settlements = settlements.Select(ToSummaryViewModel).ToList(),
            CandidatePaymentCount = candidateSummary.PaymentCount,
            CandidatePayAmount = candidateSummary.PayAmount,
            PlatformFeeRate = PlatformFeeRate
        };
    }

    public async Task<SettlementCandidatesViewModel> GetCandidatesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SettlementCandidate> candidates =
            await settlementRepository.GetSettlementCandidatesAsync(cancellationToken);

        var groups = candidates
            .GroupBy(item => new { item.RunnerId, item.RunnerName })
            .Select(group =>
            {
                decimal total = group.Sum(item => item.PayAmount);
                decimal fee = decimal.Round(total * PlatformFeeRate, 2, MidpointRounding.AwayFromZero);

                return new SettlementRunnerGroupViewModel
                {
                    RunnerId = group.Key.RunnerId,
                    RunnerName = group.Key.RunnerName,
                    PaymentCount = group.Count(),
                    OrderTotal = total,
                    PlatformFee = fee,
                    NetIncome = total - fee,
                    Payments = group.Select(candidate => SettlementCandidateViewModel.FromModel(
                        candidate,
                        DisplayNameService.GetPayMethodName(candidate.PayMethod))).ToList()
                };
            })
            .OrderByDescending(group => group.OrderTotal)
            .ThenBy(group => group.RunnerId)
            .ToList();

        return new SettlementCandidatesViewModel
        {
            RunnerGroups = groups,
            PlatformFeeRate = PlatformFeeRate
        };
    }

    public async Task<SettlementDetailsViewModel?> GetDetailsAsync(
        int settlementId,
        CancellationToken cancellationToken = default)
    {
        Settlement? settlement = await settlementRepository.GetByIdAsync(settlementId, cancellationToken);
        if (settlement == null)
        {
            return null;
        }

        IReadOnlyList<SettlementPaymentItem> items =
            await settlementRepository.GetItemsAsync(settlementId, cancellationToken);

        return new SettlementDetailsViewModel
        {
            Settlement = ToSummaryViewModel(settlement),
            Items = items.Select(item => SettlementPaymentItemViewModel.FromModel(
                item,
                DisplayNameService.GetPayMethodName(item.PayMethod))).ToList()
        };
    }

    public async Task<RunnerSettlementIndexViewModel> GetRunnerSettlementsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        RunnerSettlementSummary summary =
            await settlementRepository.GetRunnerSettlementSummaryAsync(userId, cancellationToken);
        IReadOnlyList<Settlement> settlements =
            await settlementRepository.GetSettlementsByRunnerUserIdAsync(userId, cancellationToken);

        return new RunnerSettlementIndexViewModel
        {
            Settlements = settlements.Select(ToSummaryViewModel).ToList(),
            SettlementCount = summary.SettlementCount,
            WaitingCount = summary.WaitingCount,
            DoneCount = summary.DoneCount,
            BlockedCount = summary.BlockedCount,
            TotalNetIncome = summary.TotalNetIncome,
            WaitingNetIncome = summary.WaitingNetIncome,
            DoneNetIncome = summary.DoneNetIncome
        };
    }

    public async Task<RunnerSettlementDetailsViewModel?> GetRunnerSettlementDetailsAsync(
        int settlementId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Settlement? settlement =
            await settlementRepository.GetByIdForRunnerUserAsync(settlementId, userId, cancellationToken);
        if (settlement is null)
        {
            return null;
        }

        IReadOnlyList<SettlementPaymentItem> items =
            await settlementRepository.GetItemsForRunnerUserAsync(settlementId, userId, cancellationToken);

        return new RunnerSettlementDetailsViewModel
        {
            Settlement = ToSummaryViewModel(settlement),
            Items = items.Select(item => SettlementPaymentItemViewModel.FromModel(
                item,
                DisplayNameService.GetPayMethodName(item.PayMethod))).ToList()
        };
    }

    public async Task<SettlementOperationResult> GenerateForRunnerAsync(
        int runnerId,
        CancellationToken cancellationToken = default)
    {
        if (runnerId <= 0)
        {
            return new SettlementOperationResult(false, "跑腿员编号无效。", null);
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            IReadOnlyList<SettlementCandidate> candidates =
                await settlementRepository.GetSettlementCandidatesForRunnerWithLockAsync(
                    runnerId,
                    transaction,
                    cancellationToken);

            if (candidates.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SettlementOperationResult(false, "该跑腿员当前没有可结算支付记录。", null);
            }

            decimal total = candidates.Sum(item => item.PayAmount);
            decimal platformFee = decimal.Round(total * PlatformFeeRate, 2, MidpointRounding.AwayFromZero);
            var settlement = new Settlement
            {
                RunnerId = runnerId,
                OrderTotal = total,
                PlatformFee = platformFee,
                NetIncome = total - platformFee,
                SettlementStatus = SettlementStatusCodes.Waiting
            };

            int settlementId = await settlementRepository.InsertSettlementAsync(
                settlement,
                transaction,
                cancellationToken);

            foreach (SettlementCandidate candidate in candidates)
            {
                await settlementRepository.InsertSettlementItemAsync(
                    settlementId,
                    candidate.PaymentId,
                    transaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new SettlementOperationResult(true, "结算单已生成。", settlementId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SettlementOperationResult> ChangeStatusAsync(
        int settlementId,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (settlementId <= 0)
        {
            return new SettlementOperationResult(false, "结算单编号无效。", settlementId);
        }

        status = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim().ToUpperInvariant();
        if (!SettlementStatusCodes.IsKnown(status))
        {
            return new SettlementOperationResult(false, "结算状态无效。", settlementId);
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            Settlement? existing = await settlementRepository.GetByIdWithLockAsync(
                settlementId,
                transaction,
                cancellationToken);
            if (existing is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SettlementOperationResult(false, "未找到对应的结算单。", settlementId);
            }

            if (existing.SettlementStatus == status)
            {
                await transaction.CommitAsync(cancellationToken);
                return new SettlementOperationResult(
                    true,
                    $"结算单已经处于“{DisplayNameService.GetSettlementStatusName(status)}”状态，无需重复更新。",
                    settlementId);
            }

            if (!SettlementStatusCodes.CanTransition(existing.SettlementStatus, status))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SettlementOperationResult(
                    false,
                    $"不能从“{DisplayNameService.GetSettlementStatusName(existing.SettlementStatus)}”变更为“{DisplayNameService.GetSettlementStatusName(status)}”。已结算为终态，阻断状态需先恢复为待结算。",
                    settlementId);
            }

            if (!await settlementRepository.UpdateStatusAsync(
                    settlementId,
                    existing.SettlementStatus,
                    status,
                    transaction,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SettlementOperationResult(false, "结算状态已被其他操作修改，请刷新后重试。", settlementId);
            }

            await transaction.CommitAsync(cancellationToken);
            return new SettlementOperationResult(true, "结算状态已更新。", settlementId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static SettlementSummaryViewModel ToSummaryViewModel(Settlement settlement)
    {
        return SettlementSummaryViewModel.FromModel(
            settlement,
            DisplayNameService.GetSettlementStatusName(settlement.SettlementStatus));
    }
}
