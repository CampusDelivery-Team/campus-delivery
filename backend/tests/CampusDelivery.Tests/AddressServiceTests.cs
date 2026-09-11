using CampusDelivery.Api.Models;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Tests;

public sealed class AddressServiceTests
{
    [Fact]
    public async Task AddAddressAsync_WhenTwoRequestsCompete_AssignsDistinctNumbersAndOneDefault()
    {
        var repository = new FakeAddressRepository();
        var transactions = new FakeRepositoryTransactionManager();
        var service = new AddressService(repository, transactions);

        Task<(bool Success, string ErrorMessage)> first = service.AddAddressAsync(NewAddress("同济大学", "1号楼101"));
        Task<(bool Success, string ErrorMessage)> second = service.AddAddressAsync(NewAddress("同济大学", "2号楼202"));
        var results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.Success));
        IReadOnlyList<UserAddress> addresses = repository.Snapshot();
        Assert.Equal([1, 2], addresses.Select(item => item.AddressNo).OrderBy(value => value));
        Assert.Single(addresses, item => item.IsDefault == "Y");
        Assert.All(transactions.Transactions, transaction => Assert.True(transaction.WasCommitted));
    }

    [Fact]
    public async Task SetDefaultAsync_WhenTargetDoesNotExist_DoesNotClearCurrentDefault()
    {
        var repository = new FakeAddressRepository();
        repository.Seed(NewAddress("同济大学", "1号楼101", 1, "Y"));
        var transactions = new FakeRepositoryTransactionManager();
        var service = new AddressService(repository, transactions);

        var result = await service.SetDefaultAsync(501, 99);

        Assert.False(result.Success);
        Assert.Equal("Y", repository.Snapshot().Single().IsDefault);
        Assert.True(transactions.Transactions.Single().WasRolledBack);
    }

    [Fact]
    public async Task SetDefaultAsync_WhenTargetExists_SwitchesDefaultAtomically()
    {
        var repository = new FakeAddressRepository();
        repository.Seed(NewAddress("同济大学", "1号楼101", 1, "Y"));
        repository.Seed(NewAddress("同济大学", "2号楼202", 2, "N"));
        var transactions = new FakeRepositoryTransactionManager();
        var service = new AddressService(repository, transactions);

        var result = await service.SetDefaultAsync(501, 2);

        Assert.True(result.Success);
        IReadOnlyList<UserAddress> addresses = repository.Snapshot();
        Assert.Single(addresses, address => address.IsDefault == "Y");
        Assert.Equal("Y", addresses.Single(address => address.AddressNo == 2).IsDefault);
        Assert.True(transactions.Transactions.Single().WasCommitted);
    }

    [Fact]
    public async Task DeleteAddressAsync_WhenDeletingDefault_PromotesRemainingAddress()
    {
        var repository = new FakeAddressRepository();
        repository.Seed(NewAddress("同济大学", "1号楼101", 1, "Y"));
        repository.Seed(NewAddress("同济大学", "2号楼202", 2, "N"));
        var transactions = new FakeRepositoryTransactionManager();
        var service = new AddressService(repository, transactions);

        var result = await service.DeleteAddressAsync(501, 1);

        Assert.True(result.Success);
        UserAddress remaining = Assert.Single(repository.Snapshot());
        Assert.Equal(2, remaining.AddressNo);
        Assert.Equal("Y", remaining.IsDefault);
        Assert.True(transactions.Transactions.Single().WasCommitted);
    }

    private static UserAddress NewAddress(
        string campus,
        string room,
        int addressNo = 0,
        string isDefault = "N") => new()
        {
            UserId = 501,
            AddressNo = addressNo,
            ContactName = "测试用户",
            ContactPhone = "13800000000",
            Campus = campus,
            BuildingRoom = room,
            IsDefault = isDefault
        };
}
