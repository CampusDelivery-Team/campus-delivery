using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface ISettlementService
{
    Task<SettlementIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<SettlementCandidatesViewModel> GetCandidatesAsync(CancellationToken cancellationToken = default);
    Task<SettlementDetailsViewModel?> GetDetailsAsync(int settlementId, CancellationToken cancellationToken = default);
    Task<RunnerSettlementIndexViewModel> GetRunnerSettlementsAsync(int userId, CancellationToken cancellationToken = default);
    Task<RunnerSettlementDetailsViewModel?> GetRunnerSettlementDetailsAsync(int settlementId, int userId, CancellationToken cancellationToken = default);
    Task<SettlementOperationResult> GenerateForRunnerAsync(int runnerId, CancellationToken cancellationToken = default);
    Task<SettlementOperationResult> ChangeStatusAsync(int settlementId, string status, CancellationToken cancellationToken = default);
}
