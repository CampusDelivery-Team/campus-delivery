using System.ComponentModel.DataAnnotations;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Tests;

public sealed class TaskServiceTests
{
    [Theory]
    [InlineData("FOOD", nameof(TaskCreateViewModel.MerchantName))]
    [InlineData("EXPRESS", nameof(TaskCreateViewModel.ExpressCompany))]
    [InlineData("EXPRESS", nameof(TaskCreateViewModel.WaybillNo))]
    [InlineData("EXPRESS", nameof(TaskCreateViewModel.PickupCode))]
    [InlineData("PRIVATE", nameof(TaskCreateViewModel.ItemCategory))]
    [InlineData("PRIVATE", nameof(TaskCreateViewModel.PickupLocation))]
    [InlineData("PRIVATE", nameof(TaskCreateViewModel.DeliveryLocation))]
    public void Validate_WhenRequiredTypedDetailIsMissing_ReturnsFieldError(
        string taskKind,
        string missingField)
    {
        TaskCreateViewModel model = CreateValidModel(taskKind);
        typeof(TaskCreateViewModel).GetProperty(missingField)!.SetValue(model, null);

        List<ValidationResult> results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(missingField));
    }

    [Theory]
    [InlineData("FOOD")]
    [InlineData("EXPRESS")]
    [InlineData("PRIVATE")]
    public void Validate_WhenTypedDetailsAreComplete_HasNoValidationErrors(string taskKind)
    {
        TaskCreateViewModel model = CreateValidModel(taskKind);

        List<ValidationResult> results = Validate(model);

        Assert.Empty(results);
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
            new NotUsedServiceTypeRepository(),
            new NotUsedNodeRepository());

    private static TaskCreateViewModel CreateValidModel(string taskKind) => new()
    {
        TaskKind = taskKind,
        ServiceTypeId = 1,
        AddressNo = 1,
        NodeId = 1,
        TaskTitle = "测试任务",
        TaskPrice = 10m,
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
}
