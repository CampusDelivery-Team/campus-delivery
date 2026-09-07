using CampusDelivery.Api.Models;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task SubmitPaymentAsync_WhenPayMethodIsInvalid_IsRejectedBeforeTransaction()
    {
        PaymentFixture fixture = CreateFixture(receiptConfirmed: true);

        var result = await fixture.Service.SubmitPaymentAsync(
            fixture.AssignRepository.TaskId,
            fixture.AssignRepository.PublisherUserId,
            "BANK_CARD");

        Assert.False(result.Success);
        Assert.Empty(fixture.TransactionManager.Transactions);
        Assert.Equal(0, fixture.PaymentRepository.InsertCount);
    }

    [Fact]
    public async Task SubmitPaymentAsync_WhenReceiptIsNotConfirmed_IsRejected()
    {
        PaymentFixture fixture = CreateFixture(receiptConfirmed: false);

        var result = await fixture.Service.SubmitPaymentAsync(
            fixture.AssignRepository.TaskId,
            fixture.AssignRepository.PublisherUserId,
            "WECHAT");

        Assert.False(result.Success);
        Assert.Contains("确认收货", result.ErrorMessage);
        Assert.Equal("WAIT_CONFIRM", fixture.AssignRepository.TaskStatus);
        Assert.Equal(0, fixture.PaymentRepository.InsertCount);
        Assert.True(fixture.TransactionManager.Transactions.Single().WasRolledBack);
    }

    [Fact]
    public async Task SubmitPaymentAsync_WhenReceiptIsConfirmed_CompletesTaskAndReleasesRunner()
    {
        PaymentFixture fixture = CreateFixture(receiptConfirmed: true);

        var result = await fixture.Service.SubmitPaymentAsync(
            fixture.AssignRepository.TaskId,
            fixture.AssignRepository.PublisherUserId,
            "ALIPAY");

        Assert.True(result.Success);
        Assert.Equal(701, result.PaymentId);
        Assert.Equal("PAID", fixture.PaymentRepository.ExistingPayment?.PayStatus);
        Assert.Equal("FINISHED", fixture.AssignRepository.TaskStatus);
        Assert.Equal("FREE", fixture.AssignRepository.GetRunner(1011)?.WorkStatus);
        Assert.Contains(
            fixture.AssignRepository.InsertedStatusLogs,
            log => log.StatusBefore == "WAIT_CONFIRM" && log.StatusAfter == "FINISHED");
        Assert.True(fixture.TransactionManager.Transactions.Single().WasCommitted);
    }

    [Fact]
    public async Task SubmitPaymentAsync_WhenRunnerHasOtherTask_KeepsRunnerBusy()
    {
        PaymentFixture fixture = CreateFixture(receiptConfirmed: true);
        fixture.AssignRepository.OtherActiveTaskCount = 1;

        var result = await fixture.Service.SubmitPaymentAsync(
            fixture.AssignRepository.TaskId,
            fixture.AssignRepository.PublisherUserId,
            "ALIPAY");

        Assert.True(result.Success);
        Assert.Equal("FINISHED", fixture.AssignRepository.TaskStatus);
        Assert.Equal("BUSY", fixture.AssignRepository.GetRunner(1011)?.WorkStatus);
    }

    [Fact]
    public async Task SaveUnpaidPaymentAsync_KeepsTaskPendingPaymentAndReleasesRunner()
    {
        PaymentFixture fixture = CreateFixture(receiptConfirmed: true);

        var result = await fixture.Service.SaveUnpaidPaymentAsync(
            fixture.AssignRepository.TaskId,
            fixture.AssignRepository.PublisherUserId,
            "WECHAT");

        Assert.True(result.Success);
        Assert.Equal(PaymentStatusCodes.Unpaid, fixture.PaymentRepository.ExistingPayment?.PayStatus);
        Assert.Equal(TaskStatusCodes.WaitConfirm, fixture.AssignRepository.TaskStatus);
        Assert.Equal("FREE", fixture.AssignRepository.GetRunner(1011)?.WorkStatus);
        Assert.DoesNotContain(
            fixture.AssignRepository.InsertedStatusLogs,
            log => log.StatusAfter == TaskStatusCodes.Finished);
        Assert.True(fixture.TransactionManager.Transactions.Single().WasCommitted);
    }

    [Fact]
    public async Task SubmitPaymentAsync_WhenPaymentWasRefunded_CannotPayAgain()
    {
        PaymentFixture fixture = CreateFixture(receiptConfirmed: true);
        fixture.AssignRepository.TaskStatus = "FINISHED";
        fixture.PaymentRepository.ExistingPayment = new PaymentRecord
        {
            PaymentId = 701,
            TaskId = fixture.AssignRepository.TaskId,
            RecordId = 901,
            PublisherUserId = fixture.AssignRepository.PublisherUserId,
            OrderAmount = 12.5m,
            PayAmount = 12.5m,
            PayMethod = "WECHAT",
            PayStatus = "REFUNDED"
        };

        var result = await fixture.Service.SubmitPaymentAsync(
            fixture.AssignRepository.TaskId,
            fixture.AssignRepository.PublisherUserId,
            "WECHAT");

        Assert.False(result.Success);
        Assert.Equal(0, fixture.PaymentRepository.InsertCount);
        Assert.Equal("REFUNDED", fixture.PaymentRepository.ExistingPayment.PayStatus);
    }

    private static PaymentFixture CreateFixture(bool receiptConfirmed)
    {
        var assignRepository = new FakeAssignRepository
        {
            TaskStatus = "WAIT_CONFIRM",
            ReceiptConfirmed = receiptConfirmed,
            LatestAssignRecord = new AssignRecord
            {
                RecordId = 901,
                TaskId = 101,
                RunnerId = 1011,
                OperationType = "SELF"
            }
        };
        assignRepository.AddRunner(userId: 11, runnerId: 1011, workStatus: "BUSY");

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
                    TaskStatus = "WAIT_CONFIRM"
                }
            }
        };
        var paymentRepository = new FakePaymentRepository();
        var transactionManager = new FakeRepositoryTransactionManager();
        var service = new PaymentService(
            taskRepository,
            assignRepository,
            paymentRepository,
            new FakeRefundRepository(),
            new FakeSettlementRepository(),
            transactionManager);

        return new PaymentFixture(
            service,
            assignRepository,
            paymentRepository,
            transactionManager);
    }

    private sealed record PaymentFixture(
        PaymentService Service,
        FakeAssignRepository AssignRepository,
        FakePaymentRepository PaymentRepository,
        FakeRepositoryTransactionManager TransactionManager);
}
