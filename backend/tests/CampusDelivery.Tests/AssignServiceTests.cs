using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Tests;

public sealed class AssignServiceTests
{
    [Fact]
    public async Task GrabTaskAsync_WhenTwoRunnersCompete_OnlyOneSucceeds()
    {
        var repository = new FakeAssignRepository();
        repository.AddRunner(userId: 11, runnerId: 1011);
        repository.AddRunner(userId: 12, runnerId: 1012);
        var transactionManager = new FakeRepositoryTransactionManager();
        var service = new AssignService(repository, transactionManager);

        bool[] results = await Task.WhenAll(
            service.GrabTaskAsync(repository.TaskId, 11),
            service.GrabTaskAsync(repository.TaskId, 12));

        Assert.Equal(1, results.Count(success => success));
        Assert.Equal("ASSIGNED", repository.TaskStatus);
        Assert.Single(repository.InsertedAssignRecords);
        Assert.Single(repository.InsertedStatusLogs);
        Assert.Equal(1, new[] { 1011, 1012 }.Count(id => repository.GetRunner(id)?.WorkStatus == "BUSY"));
    }

    [Fact]
    public async Task GrabTaskAsync_WhenRunnerPublishedTask_IsRejectedWithoutWrites()
    {
        var repository = new FakeAssignRepository { PublisherUserId = 11 };
        repository.AddRunner(userId: 11, runnerId: 1011);
        var service = new AssignService(repository, new FakeRepositoryTransactionManager());

        bool result = await service.GrabTaskAsync(repository.TaskId, 11);

        Assert.False(result);
        Assert.Equal("WAITING", repository.TaskStatus);
        Assert.Equal("FREE", repository.GetRunner(1011)?.WorkStatus);
        Assert.Empty(repository.InsertedAssignRecords);
        Assert.Empty(repository.InsertedStatusLogs);
    }

    [Fact]
    public async Task AssignTaskAsync_WhenRunnerPublishedTask_IsRejectedWithoutWrites()
    {
        var repository = new FakeAssignRepository { PublisherUserId = 11 };
        repository.AddRunner(userId: 11, runnerId: 1011);
        var service = new AssignService(repository, new FakeRepositoryTransactionManager());

        bool result = await service.AssignTaskAsync(repository.TaskId, 1011, adminUserId: 9001);

        Assert.False(result);
        Assert.Equal("WAITING", repository.TaskStatus);
        Assert.Equal("FREE", repository.GetRunner(1011)?.WorkStatus);
        Assert.Empty(repository.InsertedAssignRecords);
    }

    [Fact]
    public async Task ReassignTaskAsync_WhenNewRunnerPublishedTask_IsRejectedWithoutWrites()
    {
        var repository = CreateAssignedRepository();
        repository.AddRunner(userId: repository.PublisherUserId, runnerId: 2022);
        var service = new AssignService(repository, new FakeRepositoryTransactionManager());

        bool result = await service.ReassignTaskAsync(
            repository.TaskId,
            newRunnerId: 2022,
            reason: "测试重派",
            adminUserId: 9001);

        Assert.False(result);
        Assert.Equal("ASSIGNED", repository.TaskStatus);
        Assert.Equal("BUSY", repository.GetRunner(1011)?.WorkStatus);
        Assert.Equal("FREE", repository.GetRunner(2022)?.WorkStatus);
        Assert.Empty(repository.InsertedAssignRecords);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenTransitionSkipsStep_IsRejected()
    {
        var repository = CreateAssignedRepository();
        var service = new AssignService(repository, new FakeRepositoryTransactionManager());

        bool result = await service.UpdateStatusAsync(repository.TaskId, "DELIVERING", 11);

        Assert.False(result);
        Assert.Equal("ASSIGNED", repository.TaskStatus);
        Assert.Empty(repository.InsertedStatusLogs);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenTransitionsAreOrdered_ReachesWaitConfirm()
    {
        var repository = CreateAssignedRepository();
        var service = new AssignService(repository, new FakeRepositoryTransactionManager());

        Assert.True(await service.UpdateStatusAsync(repository.TaskId, "PICKED_UP", 11));
        Assert.True(await service.UpdateStatusAsync(repository.TaskId, "DELIVERING", 11));
        Assert.True(await service.UpdateStatusAsync(repository.TaskId, "WAIT_CONFIRM", 11));

        Assert.Equal("WAIT_CONFIRM", repository.TaskStatus);
        Assert.Equal(3, repository.InsertedStatusLogs.Count);
        Assert.Equal(
            new[] { "PICKED_UP", "DELIVERING", "WAIT_CONFIRM" },
            repository.InsertedStatusLogs.Select(log => log.StatusAfter));
    }

    [Fact]
    public async Task ConfirmReceiptAsync_WhenSubmittedTwice_IsIdempotent()
    {
        var repository = CreateAssignedRepository();
        repository.TaskStatus = "WAIT_CONFIRM";
        var service = new AssignService(repository, new FakeRepositoryTransactionManager());

        bool first = await service.ConfirmReceiptAsync(repository.TaskId, repository.PublisherUserId);
        bool second = await service.ConfirmReceiptAsync(repository.TaskId, repository.PublisherUserId);

        Assert.True(first);
        Assert.False(second);
        Assert.True(repository.ReceiptConfirmed);
        Assert.Single(repository.InsertedStatusLogs);
    }

    [Fact]
    public async Task GetMyTasksAsync_ForExpressTask_IncludesPickupCodeAndRequirements()
    {
        var repository = CreateAssignedRepository();
        repository.ActiveTasks.Add(new CampusTask
        {
            TaskId = repository.TaskId,
            PublisherUserId = repository.PublisherUserId,
            ServiceTypeId = 21,
            AddressNo = 1,
            NodeId = 23,
            TaskTitle = "帮我取快递",
            TaskPrice = 6m,
            TaskStatus = "ASSIGNED"
        });
        repository.ActiveTaskDetails = new TaskDetailsRecord
        {
            Task = new TaskRecord { TaskId = repository.TaskId, TaskKind = "EXPRESS" },
            ExpressCompany = "顺丰",
            WaybillNo = "SF123456",
            PickupCode = "8-2-301",
            ExpressPickupNote = "易碎，请轻拿轻放"
        };
        var service = new AssignService(repository, new FakeRepositoryTransactionManager());

        MyTasksViewModel? result = await service.GetMyTasksAsync(11, 1, 20);

        MyTaskItemViewModel task = Assert.Single(Assert.IsType<MyTasksViewModel>(result).ActiveTasks);
        Assert.Contains(task.DetailFields, field => field.Label == "取件码" && field.Value == "8-2-301");
        Assert.Contains(task.DetailFields, field => field.Label == "物流单号" && field.Value == "SF123456");
        Assert.Contains(task.DetailFields, field => field.Label == "取件备注" && field.Value == "易碎，请轻拿轻放");
    }

    private static FakeAssignRepository CreateAssignedRepository()
    {
        var repository = new FakeAssignRepository { TaskStatus = "ASSIGNED" };
        repository.AddRunner(userId: 11, runnerId: 1011, workStatus: "BUSY");
        repository.LatestAssignRecord = new AssignRecord
        {
            RecordId = 901,
            TaskId = repository.TaskId,
            RunnerId = 1011,
            OperationType = "SELF"
        };
        return repository;
    }
}
