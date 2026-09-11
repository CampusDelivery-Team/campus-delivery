using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IAddressRepository
{
    List<UserAddress> GetAddressesByUserId(int userId);
    UserAddress? GetAddress(int userId, int addressNo);
    Task<bool> LockUserAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> GetNextAddressNoAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> InsertAddressAsync(UserAddress address, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<UserAddress?> GetAddressWithLockAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateAddressAsync(UserAddress address, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<DefaultAddressProcedureResult> SetDefaultAddressAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> DeleteAddressAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> SetFirstAddressAsDefaultAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
}
