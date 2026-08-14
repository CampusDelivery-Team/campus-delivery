using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class SettlementService(
    SettlementRepository settlementRepository,
    OracleConnectionFactory connectionFactory)
{
    public const decimal PlatformFeeRate = 0.10m;

    public async Task<SettlementIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Settlement> settlements = await settlementRepository.GetRecentSettlementsAsync(cancellationToken);
        SettlementCandidateSummary candidateSummary =
            await settlementRepository.GetSettlementCandidateSummaryAsync(cancellationToken);

        return new SettlementIndexViewModel
        {
            Settlements = settlements.Select(SettlementSummaryViewModel.FromModel).ToList(),
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
                    Payments = group.Select(SettlementCandidateViewModel.FromModel).ToList()
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
            Settlement = SettlementSummaryViewModel.FromModel(settlement),
            Items = items.Select(SettlementPaymentItemViewModel.FromModel).ToList()
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
            Settlements = settlements.Select(SettlementSummaryViewModel.FromModel).ToList(),
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
            Settlement = SettlementSummaryViewModel.FromModel(settlement),
            Items = items.Select(SettlementPaymentItemViewModel.FromModel).ToList()
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

        await using OracleConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using OracleTransaction transaction =
            (OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            IReadOnlyList<SettlementCandidate> candidates =
                await settlementRepository.GetSettlementCandidatesForRunnerWithLockAsync(
                    runnerId,
                    connection,
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
                SettlementStatus = "WAITING"
            };

            int settlementId = await settlementRepository.InsertSettlementAsync(
                settlement,
                connection,
                transaction,
                cancellationToken);

            foreach (SettlementCandidate candidate in candidates)
            {
                await settlementRepository.InsertSettlementItemAsync(
                    settlementId,
                    candidate.PaymentId,
                    connection,
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
        if (status is not ("WAITING" or "DONE" or "BLOCKED"))
        {
            return new SettlementOperationResult(false, "结算状态无效。", settlementId);
        }

        Settlement? existing = await settlementRepository.GetByIdAsync(settlementId, cancellationToken);
        if (existing is null)
        {
            return new SettlementOperationResult(false, "未找到对应的结算单。", settlementId);
        }

        await settlementRepository.UpdateStatusAsync(settlementId, status, cancellationToken);
        return new SettlementOperationResult(true, "结算状态已更新。", settlementId);
    }
}

public sealed record SettlementOperationResult(bool Success, string Message, int? SettlementId);
