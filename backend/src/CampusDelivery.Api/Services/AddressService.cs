using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories;

namespace CampusDelivery.Api.Services;

public sealed class AddressService(AddressRepository addressRepository)
{
    public List<UserAddress> GetUserAddresses(int userId)
    {
        return addressRepository.GetAddressesByUserId(userId);
    }

    public UserAddress? GetAddress(int userId, int addressNo)
    {
        return addressRepository.GetAddress(userId, addressNo);
    }

    public (bool Success, string ErrorMessage) AddAddress(UserAddress address)
    {
        var addressNo = addressRepository.InsertAddress(address);
        if (addressNo <= 0)
        {
            return (false, "地址保存失败，请稍后再试");
        }

        if (address.IsDefault == "Y" || addressRepository.GetAddressesByUserId(address.UserId).Count == 1)
        {
            addressRepository.SetDefaultAddress(address.UserId, addressNo);
        }

        return (true, string.Empty);
    }

    public (bool Success, string ErrorMessage) UpdateAddress(UserAddress address)
    {
        if (!addressRepository.UpdateAddress(address))
        {
            return (false, "地址不存在或已被删除");
        }

        if (address.IsDefault == "Y")
        {
            addressRepository.SetDefaultAddress(address.UserId, address.AddressNo);
        }

        return (true, string.Empty);
    }

    public bool DeleteAddress(int userId, int addressNo)
    {
        return addressRepository.DeleteAddress(userId, addressNo);
    }

    public bool SetDefault(int userId, int addressNo)
    {
        return addressRepository.SetDefaultAddress(userId, addressNo);
    }
}
