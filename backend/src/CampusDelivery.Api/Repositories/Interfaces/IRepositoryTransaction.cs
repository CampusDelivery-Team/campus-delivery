namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IRepositoryTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IRepositoryTransactionManager
{
    Task<IRepositoryTransaction> BeginAsync(CancellationToken cancellationToken = default);
}
