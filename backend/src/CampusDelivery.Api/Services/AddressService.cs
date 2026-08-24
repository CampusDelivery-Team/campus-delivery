using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class AddressService(
    IAddressRepository addressRepository,
    IRepositoryTransactionManager transactionManager) : IAddressService
{
    public List<UserAddress> GetUserAddresses(int userId) =>
        addressRepository.GetAddressesByUserId(userId);

    public UserAddress? GetAddress(int userId, int addressNo) =>
        addressRepository.GetAddress(userId, addressNo);

    public async Task<(bool Success, string ErrorMessage)> AddAddressAsync(
        UserAddress address,
        CancellationToken cancellationToken = default)
    {
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            if (!await addressRepository.LockUserAsync(address.UserId, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "当前用户不存在，无法保存地址");
            }

            int nextAddressNo = await addressRepository.GetNextAddressNoAsync(
                address.UserId,
                transaction,
                cancellationToken);
            bool makeDefault = address.IsDefault == "Y" || nextAddressNo == 1;
            if (makeDefault)
            {
                await addressRepository.ClearDefaultAddressesAsync(address.UserId, transaction, cancellationToken);
            }

            address.AddressNo = nextAddressNo;
            address.IsDefault = makeDefault ? "Y" : "N";
            if (!await addressRepository.InsertAddressAsync(address, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "地址保存失败，请稍后再试");
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string ErrorMessage)> UpdateAddressAsync(
        UserAddress address,
        CancellationToken cancellationToken = default)
    {
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            if (!await addressRepository.LockUserAsync(address.UserId, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "当前用户不存在，无法修改地址");
            }

            UserAddress? existing = await addressRepository.GetAddressWithLockAsync(
                address.UserId,
                address.AddressNo,
                transaction,
                cancellationToken);
            if (existing == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "地址不存在或已被删除");
            }

            if (!await addressRepository.UpdateAddressAsync(address, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "地址修改失败，请稍后重试");
            }

            if (address.IsDefault == "Y" && existing.IsDefault != "Y")
            {
                await addressRepository.ClearDefaultAddressesAsync(address.UserId, transaction, cancellationToken);
                if (!await addressRepository.SetDefaultAddressAsync(
                        address.UserId,
                        address.AddressNo,
                        transaction,
                        cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, "默认地址设置失败，本次修改未保存");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string ErrorMessage)> DeleteAddressAsync(
        int userId,
        int addressNo,
        CancellationToken cancellationToken = default)
    {
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            if (!await addressRepository.LockUserAsync(userId, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "当前用户不存在，无法删除地址");
            }

            UserAddress? existing = await addressRepository.GetAddressWithLockAsync(
                userId,
                addressNo,
                transaction,
                cancellationToken);
            if (existing == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "地址不存在或已被删除");
            }

            if (!await addressRepository.DeleteAddressAsync(userId, addressNo, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "地址删除失败，请稍后重试");
            }

            if (existing.IsDefault == "Y")
            {
                await addressRepository.SetFirstAddressAsDefaultAsync(userId, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string ErrorMessage)> SetDefaultAsync(
        int userId,
        int addressNo,
        CancellationToken cancellationToken = default)
    {
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            if (!await addressRepository.LockUserAsync(userId, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "当前用户不存在，无法设置默认地址");
            }

            UserAddress? target = await addressRepository.GetAddressWithLockAsync(
                userId,
                addressNo,
                transaction,
                cancellationToken);
            if (target == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "目标地址不存在，原默认地址保持不变");
            }

            if (target.IsDefault == "Y")
            {
                await transaction.CommitAsync(cancellationToken);
                return (true, "该地址已经是默认地址");
            }

            await addressRepository.ClearDefaultAddressesAsync(userId, transaction, cancellationToken);
            if (!await addressRepository.SetDefaultAddressAsync(userId, addressNo, transaction, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "默认地址设置失败，原默认地址保持不变");
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
