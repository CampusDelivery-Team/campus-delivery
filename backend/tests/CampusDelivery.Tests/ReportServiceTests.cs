using System.Text;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GenerateAsync_WhenPeriodIsInvalid_RejectsBeforeTransaction()
    {
        var repository = new FakeReportRepository();
        var transactions = new FakeRepositoryTransactionManager();
        var service = new ReportService(repository, transactions);

        ReportOperationResult result = await service.GenerateAsync(new ReportGenerateViewModel
        {
            ReportType = "ORDER",
            StatPeriod = "2026年第8月"
        });

        Assert.False(result.Success);
        Assert.Empty(transactions.Transactions);
        Assert.Null(repository.StoredReport);
    }

    [Fact]
    public async Task GenerateAsync_WhenValid_SavesReportAndAuditDetailsInOneTransaction()
    {
        var repository = new FakeReportRepository
        {
            BusinessItems =
            [
                new ReportBusinessItem { BusinessId = 1, TaskId = 1, TaskTitle = "代取", PrimaryStatus = "FINISHED", Amount = 10m }
            ],
            AuditIds = [11, 12]
        };
        var transactions = new FakeRepositoryTransactionManager();
        var service = new ReportService(repository, transactions);

        ReportOperationResult result = await service.GenerateAsync(new ReportGenerateViewModel
        {
            ReportType = "ORDER",
            StatPeriod = "2026-08"
        });

        Assert.True(result.Success);
        Assert.Equal(801, result.ReportId);
        Assert.Equal([11, 12], repository.LinkedAuditIds);
        Assert.Equal(ReportStatusCodes.Generated, repository.StoredReport!.ReportStatus);
        Assert.True(transactions.Transactions.Single().WasCommitted);
    }

    [Fact]
    public async Task ExportAsync_GeneratesCsvAndMarksReportExported()
    {
        var repository = new FakeReportRepository
        {
            BusinessItems =
            [
                new ReportBusinessItem
                {
                    BusinessId = 21,
                    TaskId = 31,
                    TaskTitle = "=1+1 快递代取",
                    PrimaryStatus = PaymentStatusCodes.Paid,
                    Amount = 12.5m,
                    OccurredAt = new DateTime(2026, 8, 2),
                    Description = "WECHAT"
                }
            ]
        };
        var transactions = new FakeRepositoryTransactionManager();
        var service = new ReportService(repository, transactions);
        await service.GenerateAsync(new ReportGenerateViewModel { ReportType = "PAYMENT", StatPeriod = "2026-08" });

        var result = await service.ExportAsync(801);

        Assert.True(result.Success);
        Assert.Equal(ReportStatusCodes.Exported, repository.StoredReport!.ReportStatus);
        Assert.NotNull(result.Content);
        string csv = Encoding.UTF8.GetString(result.Content!);
        Assert.Contains("有效支付金额", csv);
        Assert.Contains("'=1+1 快递代取", csv);
    }
}
