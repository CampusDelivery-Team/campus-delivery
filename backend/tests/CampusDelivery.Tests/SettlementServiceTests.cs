using CampusDelivery.Api.Models;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Tests;

public sealed class SettlementServiceTests
{
    [Theory]
    [InlineData("WAITING", "DONE")]
    [InlineData("WAITING", "BLOCKED")]
    [InlineData("BLOCKED", "WAITING")]
    public async Task ChangeStatusAsync_WhenTransitionIsAllowed_UpdatesAndCommits(
        string currentStatus,
        string targetStatus)
    {
        var repository = new FakeSettlementRepository
        {
            Existing = new Settlement { SettlementId = 701, SettlementStatus = currentStatus }
        };
        var transactions = new FakeRepositoryTransactionManager();
        var service = new SettlementService(repository, transactions);

        var result = await service.ChangeStatusAsync(701, targetStatus);

        Assert.True(result.Success);
        Assert.Equal(targetStatus, repository.Existing.SettlementStatus);
        Assert.Equal(1, repository.UpdateCount);
        Assert.True(transactions.Transactions.Single().WasCommitted);
    }

    [Theory]
    [InlineData("DONE", "WAITING")]
    [InlineData("DONE", "BLOCKED")]
    [InlineData("BLOCKED", "DONE")]
    public async Task ChangeStatusAsync_WhenTransitionIsIllegal_RejectsWithoutUpdate(
        string currentStatus,
        string targetStatus)
    {
        var repository = new FakeSettlementRepository
        {
            Existing = new Settlement { SettlementId = 701, SettlementStatus = currentStatus }
        };
        var transactions = new FakeRepositoryTransactionManager();
        var service = new SettlementService(repository, transactions);

        var result = await service.ChangeStatusAsync(701, targetStatus);

        Assert.False(result.Success);
        Assert.Equal(currentStatus, repository.Existing.SettlementStatus);
        Assert.Equal(0, repository.UpdateCount);
        Assert.True(transactions.Transactions.Single().WasRolledBack);
    }
}
