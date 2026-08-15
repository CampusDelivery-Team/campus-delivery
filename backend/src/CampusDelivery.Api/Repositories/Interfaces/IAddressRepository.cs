using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IAddressRepository
{
    List<UserAddress> GetAddressesByUserId(int userId);
    int InsertAddress(UserAddress address);
    bool SetDefaultAddress(int userId, int addressNo);
    UserAddress? GetAddress(int userId, int addressNo);
    bool UpdateAddress(UserAddress address);
    bool DeleteAddress(int userId, int addressNo);
}
