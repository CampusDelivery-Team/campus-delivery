using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface ISettlementRepository
{
    Task<IReadOnlyList<Settlement>> GetRecentSettlementsAsync(CancellationToken cancellationToken = default);
    Task<SettlementCandidateSummary> GetSettlementCandidateSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SettlementCandidate>> GetSettlementCandidatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SettlementCandidate>> GetSettlementCandidatesForRunnerWithLockAsync(int runnerId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<Settlement?> GetByIdAsync(int settlementId, CancellationToken cancellationToken = default);
    Task<Settlement?> GetByIdWithLockAsync(int settlementId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<RunnerSettlementSummary> GetRunnerSettlementSummaryAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Settlement>> GetSettlementsByRunnerUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Settlement?> GetByIdForRunnerUserAsync(int settlementId, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SettlementPaymentItem>> GetItemsAsync(int settlementId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SettlementPaymentItem>> GetItemsForRunnerUserAsync(int settlementId, int userId, CancellationToken cancellationToken = default);
    Task<int> InsertSettlementAsync(Settlement settlement, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task InsertSettlementItemAsync(int settlementId, int paymentId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int settlementId, string currentStatus, string targetStatus, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
}
