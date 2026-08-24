using CampusDelivery.Api.Models;
using CampusDelivery.Api.Services;

namespace CampusDelivery.Tests;

public sealed class ReviewServiceTests
{
    [Theory]
    [InlineData("UNPAID")]
    [InlineData("FAILED")]
    [InlineData("REFUNDED")]
    public async Task CreateReviewAsync_WhenPaymentIsNotPaid_IsRejected(string payStatus)
    {
        var fixture = CreateFixture(payStatus);

        var result = await fixture.Service.CreateReviewAsync(101, 5, 'N', "很好", 501);

        Assert.False(result.Success);
        Assert.Contains("付款后", result.Message);
        Assert.Null(fixture.Reviews.InsertedReview);
        Assert.True(fixture.Transactions.Transactions.Single().WasRolledBack);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenRefundIsActive_IsRejected()
    {
        var fixture = CreateFixture(PaymentStatusCodes.Paid);
        fixture.Refunds.ActiveRefund = new RefundRecord
        {
            RefundId = 301,
            PaymentId = 201,
            ProcessStatus = RefundStatusCodes.Apply
        };

        var result = await fixture.Service.CreateReviewAsync(101, 5, 'N', "很好", 501);

        Assert.False(result.Success);
        Assert.Contains("退款处理中", result.Message);
        Assert.Null(fixture.Reviews.InsertedReview);
        Assert.True(fixture.Transactions.Transactions.Single().WasRolledBack);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenPaidAndNoRefund_CreatesReviewAndCreditInOneTransaction()
    {
        var fixture = CreateFixture(PaymentStatusCodes.Paid);

        var result = await fixture.Service.CreateReviewAsync(101, 5, 'N', " 很好 ", 501);

        Assert.True(result.Success);
        Assert.NotNull(fixture.Reviews.InsertedReview);
        Assert.Equal(2m, fixture.Reviews.InsertedReview!.CreditDelta);
        Assert.Equal("很好", fixture.Reviews.InsertedReview.CommentText);
        Assert.Equal(2m, fixture.Reviews.CreditChange);
        Assert.True(fixture.Transactions.Transactions.Single().WasCommitted);
    }

    private static ReviewFixture CreateFixture(string payStatus)
    {
        var reviews = new FakeReviewRepository();
        var assigns = new FakeAssignRepository
        {
            TaskStatus = TaskStatusCodes.Finished,
            PublisherUserId = 501,
            LatestAssignRecord = new AssignRecord
            {
                RecordId = 111,
                TaskId = 101,
                RunnerId = 901,
                OperationType = "SELF"
            }
        };
        assigns.AddRunner(601, 901);
        var payments = new FakePaymentRepository
        {
            ExistingPayment = new PaymentRecord
            {
                PaymentId = 201,
                TaskId = 101,
                RecordId = 111,
                PublisherUserId = 501,
                PayStatus = payStatus,
                PayAmount = 12m,
                OrderAmount = 12m
            }
        };
        var refunds = new FakeRefundRepository();
        var transactions = new FakeRepositoryTransactionManager();
        var service = new ReviewService(reviews, assigns, payments, refunds, transactions);
        return new ReviewFixture(service, reviews, refunds, transactions);
    }

    private sealed record ReviewFixture(
        ReviewService Service,
        FakeReviewRepository Reviews,
        FakeRefundRepository Refunds,
        FakeRepositoryTransactionManager Transactions);
}
