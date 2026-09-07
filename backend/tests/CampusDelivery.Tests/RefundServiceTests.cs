using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Tests;

public sealed class RefundServiceTests
{
    [Fact]
    public async Task BuildCreateModelAsync_WhenPaymentIsSettled_ReturnsNull()
    {
        RefundFixture fixture = CreateFixture(paymentSettled: true);

        RefundCreateViewModel? model = await fixture.Service.BuildCreateModelAsync(
            fixture.AssignRepository.TaskId,
            fixture.AssignRepository.PublisherUserId);

        Assert.Null(model);
    }

    [Fact]
    public async Task SubmitAsync_WhenPaymentIsSettled_IsRejectedWithoutRefundInsert()
    {
        RefundFixture fixture = CreateFixture(paymentSettled: true);

        var result = await fixture.Service.SubmitAsync(
            new RefundCreateViewModel
            {
                PaymentId = 701,
                TaskId = fixture.AssignRepository.TaskId,
                RecordId = fixture.AssignRepository.LatestAssignRecord!.RecordId,
                RefundReason = "测试退款"
            },
            fixture.AssignRepository.PublisherUserId);

        Assert.False(result.Success);
        Assert.Contains("结算流程", result.ErrorMessage);
        Assert.Equal(0, fixture.RefundRepository.InsertCount);
        Assert.Equal(TaskStatusCodes.Finished, fixture.AssignRepository.TaskStatus);
        Assert.True(fixture.TransactionManager.Transactions.Single().WasRolledBack);
    }

    [Fact]
    public async Task SubmitAsync_WhenPaymentIsNotSettled_CreatesRefund()
    {
        RefundFixture fixture = CreateFixture(paymentSettled: false);

        var result = await fixture.Service.SubmitAsync(
            new RefundCreateViewModel
            {
                PaymentId = 701,
                TaskId = fixture.AssignRepository.TaskId,
                RecordId = fixture.AssignRepository.LatestAssignRecord!.RecordId,
                RefundReason = "测试退款"
            },
            fixture.AssignRepository.PublisherUserId);

        Assert.True(result.Success);
        Assert.Equal(1, fixture.RefundRepository.InsertCount);
        Assert.Equal(TaskStatusCodes.Refunding, fixture.AssignRepository.TaskStatus);
        Assert.True(fixture.TransactionManager.Transactions.Single().WasCommitted);
    }

    private static RefundFixture CreateFixture(bool paymentSettled)
    {
        var assignRepository = new FakeAssignRepository
        {
            TaskStatus = TaskStatusCodes.Finished,
            LatestAssignRecord = new AssignRecord
            {
                RecordId = 901,
                TaskId = 101,
                RunnerId = 1011,
                OperationType = "SELF"
            }
        };

        var taskRepository = new FakeTaskRepository
        {
            Details = new TaskDetailsRecord
            {
                RecordId = 901,
                Task = new TaskRecord
                {
                    TaskId = assignRepository.TaskId,
                    TaskTitle = "测试配送任务",
                    TaskPrice = 12.5m,
                    TaskStatus = TaskStatusCodes.Finished
                }
            }
        };

        var paymentRepository = new FakePaymentRepository
        {
            ExistingPayment = new PaymentRecord
            {
                PaymentId = 701,
                TaskId = assignRepository.TaskId,
                RecordId = 901,
                PublisherUserId = assignRepository.PublisherUserId,
                OrderAmount = 12.5m,
                PayAmount = 12.5m,
                PayMethod = "WECHAT",
                PayStatus = PaymentStatusCodes.Paid
            }
        };

        var refundRepository = new FakeRefundRepository();
        var settlementRepository = new FakeSettlementRepository { PaymentSettled = paymentSettled };
        var transactionManager = new FakeRepositoryTransactionManager();
        var service = new RefundService(
            taskRepository,
            assignRepository,
            paymentRepository,
            refundRepository,
            settlementRepository,
            transactionManager);

        return new RefundFixture(
            service,
            assignRepository,
            refundRepository,
            transactionManager);
    }

    private sealed record RefundFixture(
        RefundService Service,
        FakeAssignRepository AssignRepository,
        FakeRefundRepository RefundRepository,
        FakeRepositoryTransactionManager TransactionManager);
}
