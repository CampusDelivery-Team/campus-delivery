using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IAddressService
{
    List<UserAddress> GetUserAddresses(int userId);
    UserAddress? GetAddress(int userId, int addressNo);
    Task<(bool Success, string ErrorMessage)> AddAddressAsync(UserAddress address, CancellationToken cancellationToken = default);
    Task<(bool Success, string ErrorMessage)> UpdateAddressAsync(UserAddress address, CancellationToken cancellationToken = default);
    Task<(bool Success, string ErrorMessage)> DeleteAddressAsync(int userId, int addressNo, CancellationToken cancellationToken = default);
    Task<(bool Success, string ErrorMessage)> SetDefaultAsync(int userId, int addressNo, CancellationToken cancellationToken = default);
}
