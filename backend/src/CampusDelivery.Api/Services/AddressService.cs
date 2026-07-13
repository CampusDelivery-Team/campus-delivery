using System.Collections.Generic;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories;

namespace CampusDelivery.Api.Services
{
    public class AddressService
    {
        private readonly AddressRepository _addressRepository;

        public AddressService(AddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }

        // 1. 获取用户的全部地址
        public List<UserAddress> GetUserAddresses(int userId)
        {
            return _addressRepository.GetAddressesByUserId(userId);
        }

        // 2. 获取单条地址信息（用于修改页面展示旧数据）
        public UserAddress? GetAddress(int userId, int addressNo)
        {
            return _addressRepository.GetAddress(userId, addressNo);
        }

        // 3. 新增地址核心逻辑
        public (bool Success, string ErrorMessage) AddAddress(UserAddress address)
        {
            var existingAddresses = _addressRepository.GetAddressesByUserId(address.UserId);
            if (existingAddresses.Count == 0)
            {
                address.IsDefault = "Y";
            }

            // 拿到新插入的地址序号
            int newAddressNo = _addressRepository.InsertAddress(address);
            if (newAddressNo <= 0) return (false, "系统繁忙，新增地址失败");

            // 触发排他逻辑：如果用户勾选了默认，且他之前还有别的地址，就把别的全降级
            if (address.IsDefault == "Y" && existingAddresses.Count > 0)
            {
                _addressRepository.SetDefaultAddress(address.UserId, newAddressNo);
            }

            return (true, "");
        }
        // 4. 更新修改地址
        public (bool Success, string ErrorMessage) UpdateAddress(UserAddress address)
        {
            bool isUpdated = _addressRepository.UpdateAddress(address);
            if (!isUpdated) return (false, "系统繁忙，修改地址失败");

            // 如果修改时勾选了“设为默认”，触发排他逻辑保证唯一性
            if (address.IsDefault == "Y")
            {
                _addressRepository.SetDefaultAddress(address.UserId, address.AddressNo);
            }

            return (true, "");
        }

        // 5. 独立功能：设置默认地址
        public (bool Success, string ErrorMessage) SetDefault(int userId, int addressNo)
        {
            bool isSet = _addressRepository.SetDefaultAddress(userId, addressNo);
            return isSet ? (true, "") : (false, "设置默认地址失败，请稍后再试");
        }

        // 6. 删除地址
        public (bool Success, string ErrorMessage) DeleteAddress(int userId, int addressNo)
        {
            bool isDeleted = _addressRepository.DeleteAddress(userId, addressNo);
            return isDeleted ? (true, "") : (false, "删除地址失败");
        }
    }
}
