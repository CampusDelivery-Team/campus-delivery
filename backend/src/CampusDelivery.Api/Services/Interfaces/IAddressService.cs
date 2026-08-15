using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IAddressService
{
    List<UserAddress> GetUserAddresses(int userId);
    UserAddress? GetAddress(int userId, int addressNo);
    (bool Success, string ErrorMessage) AddAddress(UserAddress address);
    (bool Success, string ErrorMessage) UpdateAddress(UserAddress address);
    bool DeleteAddress(int userId, int addressNo);
    bool SetDefault(int userId, int addressNo);
}
