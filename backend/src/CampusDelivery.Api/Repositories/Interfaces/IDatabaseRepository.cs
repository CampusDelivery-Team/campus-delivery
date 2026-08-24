namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IDatabaseRepository
{
    Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);
}
