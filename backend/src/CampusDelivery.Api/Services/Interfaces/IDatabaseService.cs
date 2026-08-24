using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IDatabaseService
{
    Task<DatabaseStatusViewModel> GetStatusAsync(CancellationToken cancellationToken = default);
}
