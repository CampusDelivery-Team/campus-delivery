using System.ComponentModel.DataAnnotations;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Tests;

public sealed class TaskServiceTests
{
    [Theory]
    [InlineData("FOOD", nameof(TaskCreateViewModel.MerchantName), "商家名称")]
    [InlineData("EXPRESS", nameof(TaskCreateViewModel.ExpressCompany), "快递公司")]
    [InlineData("EXPRESS", nameof(TaskCreateViewModel.WaybillNo), "物流单号")]
    [InlineData("EXPRESS", nameof(TaskCreateViewModel.PickupCode), "取件码")]
    [InlineData("PRIVATE", nameof(TaskCreateViewModel.ItemCategory), "物品类别")]
    [InlineData("PRIVATE", nameof(TaskCreateViewModel.PickupLocation), "取货地点")]
    [InlineData("PRIVATE", nameof(TaskCreateViewModel.DeliveryLocation), "送达地点")]
    public async Task CreateAsync_WhenRequiredTypedDetailIsMissing_ReturnsBusinessError(
        string taskKind,
        string missingField,
        string expectedMessage)
    {
        var repository = new FakeTaskRepository();
        TaskService service = CreateService(repository);
        TaskCreateViewModel model = CreateValidModel(taskKind);
        typeof(TaskCreateViewModel).GetProperty(missingField)!.SetValue(model, null);

        TaskOperationResult result = await service.CreateAsync(501, model);

        Assert.False(result.Success);
        Assert.Contains(expectedMessage, result.ErrorMessage);
        Assert.Null(repository.CapturedCreateRequest);
    }

    [Theory]
    [InlineData("FOOD")]
    [InlineData("EXPRESS")]
    [InlineData("PRIVATE")]
    public async Task CreateAsync_WhenTypedDetailsAreComplete_DerivesTaskKindFromService(string taskKind)
    {
        var repository = new FakeTaskRepository();
        TaskService service = CreateService(repository);
        TaskCreateViewModel model = CreateValidModel(taskKind);

        TaskOperationResult result = await service.CreateAsync(501, model);

        Assert.True(result.Success);
        Assert.Equal(taskKind, repository.CapturedCreateRequest?.TaskKind);
    }

    [Fact]
    public void Validate_WhenExtraAmountIsNegative_ReturnsFieldError()
    {
        TaskCreateViewModel model = CreateValidModel("FOOD");
        model.ExtraAmount = -0.01m;

        List<ValidationResult> results = Validate(model);

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(TaskCreateViewModel.ExtraAmount)));
    }

    [Fact]
    public async Task PopulateCreateOptionsAsync_MapsNodesToTheirAllowedServiceTypes()
    {
        IReadOnlyList<Node> nodes =
        [
            new() { NodeId = 10, NodeName = "南门", NodeType = "GATE", NodeStatus = "NORMAL" },
            new() { NodeId = 20, NodeName = "驿站", NodeType = "STATION", NodeStatus = "NORMAL" },
            new() { NodeId = 30, NodeName = "停用节点", NodeType = "GATE", NodeStatus = "CLOSED" }
        ];
        IReadOnlyList<ServiceNodeRule> rules =
        [
            new() { ServiceTypeId = 1, NodeId = 10, ServiceTypeStatus = "ENABLED", NodeStatus = "NORMAL" },
            new() { ServiceTypeId = 3, NodeId = 10, ServiceTypeStatus = "ENABLED", NodeStatus = "NORMAL" },
            new() { ServiceTypeId = 2, NodeId = 20, ServiceTypeStatus = "ENABLED", NodeStatus = "NORMAL" },
            new() { ServiceTypeId = 1, NodeId = 30, ServiceTypeStatus = "ENABLED", NodeStatus = "CLOSED" }
        ];
        var service = new TaskService(
            new FakeTaskRepository(),
            new FakeAddressRepository(),
            new FixedServiceTypeRepository(),
            new FixedNodeRepository(nodes),
            new FixedServiceNodeRuleRepository(rules));
        var model = new TaskCreateViewModel();

        await service.PopulateCreateOptionsAsync(model, 501);

        Assert.Collection(
            model.NodeOptions,
            option =>
            {
                Assert.Equal(10, option.Value);
                Assert.Equal([1, 3], option.AllowedServiceTypeIds);
            },
            option =>
            {
                Assert.Equal(20, option.Value);
                Assert.Equal([2], option.AllowedServiceTypeIds);
            });
    }

    [Fact]
    public async Task CreateAsync_WhenServiceNodeRuleDoesNotMatch_ReturnsBusinessMessage()
    {
        var repository = new FakeTaskRepository
        {
            CreateResult = new TaskCreateWriteResult(TaskCreateResult.RuleNotMatched)
        };
        TaskService service = CreateService(repository);

        var result = await service.CreateAsync(501, CreateValidModel("FOOD"));

        Assert.False(result.Success);
        Assert.Contains("不匹配", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceTypeHasNoKnownTaskKind_RejectsBeforeWrite()
    {
        var repository = new FakeTaskRepository();
        TaskService service = CreateService(repository);
        TaskCreateViewModel model = CreateValidModel("FOOD");
        model.ServiceTypeId = 999;

        TaskOperationResult result = await service.CreateAsync(501, model);

        Assert.False(result.Success);
        Assert.Contains("任务类型不可用", result.ErrorMessage);
        Assert.Null(repository.CapturedCreateRequest);
    }

    [Fact]
    public async Task CreateAsync_WhenDatabasePriceCalculationFails_ReturnsFriendlyMessage()
    {
        var repository = new FakeTaskRepository
        {
            CreateResult = new TaskCreateWriteResult(TaskCreateResult.PriceCalculationFailed)
        };
        TaskService service = CreateService(repository);

        var result = await service.CreateAsync(501, CreateValidModel("EXPRESS"));

        Assert.False(result.Success);
        Assert.Equal("基础费与附加费合计超出可保存金额，请降低附加费", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_NormalizesUserInputBeforeRepositoryWrite()
    {
        var repository = new FakeTaskRepository();
        TaskService service = CreateService(repository);
        TaskCreateViewModel model = CreateValidModel("FOOD");
        model.TaskTitle = "  测试外卖配送  ";
        model.MerchantName = "  测试商家  ";
        model.PlatformOrderNo = "   ";

        var result = await service.CreateAsync(501, model);

        Assert.True(result.Success);
        Assert.Equal("测试外卖配送", repository.CapturedCreateRequest?.TaskTitle);
        Assert.Equal("测试商家", repository.CapturedCreateRequest?.MerchantName);
        Assert.Null(repository.CapturedCreateRequest?.PlatformOrderNo);
        Assert.Equal(2.5m, repository.CapturedCreateRequest?.ExtraAmount);
    }

    [Fact]
    public async Task CancelAsync_WhenTaskAlreadyAdvanced_ReturnsFriendlyMessage()
    {
        var repository = new FakeTaskRepository { CancelResult = TaskCancelResult.InvalidState };
        TaskService service = CreateService(repository);

        string message = await service.CancelAsync(101, 501);

        Assert.Contains("只有待接单任务可以取消", message);
    }

    private static TaskService CreateService(FakeTaskRepository repository) =>
        new(
            repository,
            new NotUsedAddressRepository(),
            new FixedServiceTypeRepository(),
            new NotUsedNodeRepository(),
            new NotUsedServiceNodeRuleRepository());

    private static TaskCreateViewModel CreateValidModel(string taskKind) => new()
    {
        ServiceTypeId = taskKind switch
        {
            TaskKindCodes.Food => 1,
            TaskKindCodes.Express => 2,
            TaskKindCodes.Private => 3,
            _ => 999
        },
        AddressNo = 1,
        NodeId = 1,
        TaskTitle = "测试任务",
        ExtraAmount = 2.5m,
        UrgentFlag = "N",
        MerchantName = "测试商家",
        ExpressCompany = "测试快递",
        WaybillNo = "SF0001",
        PickupCode = "1234",
        ItemCategory = "文件",
        PickupLocation = "图书馆",
        DeliveryLocation = "宿舍楼"
    };

    private static List<ValidationResult> Validate(TaskCreateViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private sealed class FixedServiceTypeRepository : IServiceTypeRepository
    {
        private static readonly IReadOnlyList<ServiceType> ServiceTypes =
        [
            new() { ServiceTypeId = 1, ServiceName = "外卖分发", BasePrice = 3m, TypeStatus = "ENABLED" },
            new() { ServiceTypeId = 2, ServiceName = "快递代取", BasePrice = 4m, TypeStatus = "ENABLED" },
            new() { ServiceTypeId = 3, ServiceName = "私人跑腿", BasePrice = 5m, TypeStatus = "ENABLED" }
        ];

        public Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceTypes);

        public Task<bool> ExistsByNameAsync(string serviceName, int? excludedServiceTypeId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ServiceTypeRepositoryWriteResult> InsertAsync(ServiceType serviceType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ServiceTypeRepositoryWriteResult> UpdateAsync(ServiceType serviceType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateStatusAsync(int serviceTypeId, string typeStatus, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ServiceTypeDeleteResult> DeleteAsync(int serviceTypeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NotUsedServiceNodeRuleRepository : IServiceNodeRuleRepository
    {
        public Task<IReadOnlyList<ServiceNodeRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> InsertAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CampusDelivery.Api.Repositories.Interfaces.ServiceNodeRuleRemoveResult> RemoveAsync(
            int serviceTypeId,
            int nodeId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedNodeRepository(IReadOnlyList<Node> nodes) : INodeRepository
    {
        public Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(nodes);

        public Task<Node?> GetByIdAsync(int nodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsByNameAsync(string nodeName, int? excludedNodeId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task InsertAsync(Node node, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(Node node, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateStatusAsync(int nodeId, string nodeStatus, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NodeDeleteResult> DeleteAsync(int nodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedServiceNodeRuleRepository(IReadOnlyList<ServiceNodeRule> rules)
        : IServiceNodeRuleRepository
    {
        public Task<IReadOnlyList<ServiceNodeRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(rules);

        public Task<bool> ExistsAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> InsertAsync(int serviceTypeId, int nodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CampusDelivery.Api.Repositories.Interfaces.ServiceNodeRuleRemoveResult> RemoveAsync(
            int serviceTypeId,
            int nodeId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
