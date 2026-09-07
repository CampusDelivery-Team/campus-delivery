using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories.Interfaces;

namespace CampusDelivery.Tests;

internal sealed class FakeReviewRepository : IReviewRepository
{
    public Review? InsertedReview { get; private set; }
    public decimal CreditChange { get; private set; }
    public bool ReviewExists { get; set; }

    public Task<Review?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Review?>(null);

    public Task<ReviewWriteContext?> GetWriteContextWithLockAsync(int reviewId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) =>
        Task.FromResult<ReviewWriteContext?>(null);

    public Task<IReadOnlyList<Review>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Review>>([]);

    public Task<IReadOnlyList<Review>> GetAllPagedAsync(int offset, int pageSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Review>>([]);

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<IReadOnlyList<Review>> GetByPublisherUserIdPagedAsync(int publisherUserId, int offset, int pageSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Review>>([]);

    public Task<int> GetCountByPublisherUserIdAsync(int publisherUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<bool> ExistsByTaskIdAsync(int taskId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReviewExists);

    public Task<bool> InsertAsync(Review review, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        InsertedReview = review;
        return Task.FromResult(true);
    }

    public Task<bool> UpdateAsync(Review review, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<bool> DeleteAsync(int reviewId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<bool> UpdateRunnerCreditAsync(int runnerId, decimal creditDelta, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        CreditChange += creditDelta;
        return Task.FromResult(true);
    }
}

internal sealed class FakeAddressRepository : IAddressRepository
{
    private readonly object _sync = new();
    private readonly List<UserAddress> _addresses = [];

    public int UserId { get; set; } = 501;
    public int ClearDefaultCallCount { get; private set; }

    public IReadOnlyList<UserAddress> Snapshot()
    {
        lock (_sync)
        {
            return _addresses.Select(Clone).ToList();
        }
    }

    public void Seed(UserAddress address)
    {
        lock (_sync)
        {
            _addresses.Add(Clone(address));
        }
    }

    public List<UserAddress> GetAddressesByUserId(int userId)
    {
        lock (_sync)
        {
            return _addresses.Where(item => item.UserId == userId).Select(Clone).ToList();
        }
    }

    public UserAddress? GetAddress(int userId, int addressNo)
    {
        lock (_sync)
        {
            UserAddress? item = _addresses.SingleOrDefault(address => address.UserId == userId && address.AddressNo == addressNo);
            return item is null ? null : Clone(item);
        }
    }

    public async Task<bool> LockUserAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        await ((FakeRepositoryTransaction)transaction).AcquireTaskRowAsync(cancellationToken);
        return userId == UserId;
    }

    public Task<int> GetNextAddressNoAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            int next = _addresses.Where(item => item.UserId == userId).Select(item => item.AddressNo).DefaultIfEmpty(0).Max() + 1;
            return Task.FromResult(next);
        }
    }

    public Task<bool> InsertAddressAsync(UserAddress address, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_addresses.Any(item => item.UserId == address.UserId && item.AddressNo == address.AddressNo))
            {
                return Task.FromResult(false);
            }

            _addresses.Add(Clone(address));
            return Task.FromResult(true);
        }
    }

    public Task<UserAddress?> GetAddressWithLockAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) =>
        Task.FromResult(GetAddress(userId, addressNo));

    public Task<bool> UpdateAddressAsync(UserAddress address, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            UserAddress? existing = _addresses.SingleOrDefault(item => item.UserId == address.UserId && item.AddressNo == address.AddressNo);
            if (existing is null)
            {
                return Task.FromResult(false);
            }

            existing.ContactName = address.ContactName;
            existing.ContactPhone = address.ContactPhone;
            existing.Campus = address.Campus;
            existing.BuildingRoom = address.BuildingRoom;
            return Task.FromResult(true);
        }
    }

    public Task ClearDefaultAddressesAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            ClearDefaultCallCount++;
            foreach (UserAddress item in _addresses.Where(item => item.UserId == userId))
            {
                item.IsDefault = "N";
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> SetDefaultAddressAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            UserAddress? item = _addresses.SingleOrDefault(address => address.UserId == userId && address.AddressNo == addressNo);
            if (item is null)
            {
                return Task.FromResult(false);
            }

            item.IsDefault = "Y";
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAddressAsync(int userId, int addressNo, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_addresses.RemoveAll(item => item.UserId == userId && item.AddressNo == addressNo) == 1);
        }
    }

    public Task<bool> SetFirstAddressAsDefaultAsync(int userId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            UserAddress? item = _addresses.Where(address => address.UserId == userId).OrderBy(address => address.AddressNo).FirstOrDefault();
            if (item is null)
            {
                return Task.FromResult(false);
            }

            item.IsDefault = "Y";
            return Task.FromResult(true);
        }
    }

    private static UserAddress Clone(UserAddress address) => new()
    {
        UserId = address.UserId,
        AddressNo = address.AddressNo,
        ContactName = address.ContactName,
        ContactPhone = address.ContactPhone,
        Campus = address.Campus,
        BuildingRoom = address.BuildingRoom,
        IsDefault = address.IsDefault
    };
}

internal sealed class FakeSettlementRepository : ISettlementRepository
{
    public Settlement? Existing { get; set; }
    public int UpdateCount { get; private set; }

    public Task<IReadOnlyList<Settlement>> GetRecentSettlementsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Settlement>>([]);
    public Task<SettlementCandidateSummary> GetSettlementCandidateSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(new SettlementCandidateSummary());
    public Task<IReadOnlyList<SettlementCandidate>> GetSettlementCandidatesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SettlementCandidate>>([]);
    public Task<IReadOnlyList<SettlementCandidate>> GetSettlementCandidatesForRunnerWithLockAsync(int runnerId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SettlementCandidate>>([]);
    public Task<Settlement?> GetByIdAsync(int settlementId, CancellationToken cancellationToken = default) => Task.FromResult(Existing?.SettlementId == settlementId ? Existing : null);
    public Task<Settlement?> GetByIdWithLockAsync(int settlementId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => GetByIdAsync(settlementId, cancellationToken);
    public Task<RunnerSettlementSummary> GetRunnerSettlementSummaryAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(new RunnerSettlementSummary());
    public Task<IReadOnlyList<Settlement>> GetSettlementsByRunnerUserIdAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Settlement>>([]);
    public Task<Settlement?> GetByIdForRunnerUserAsync(int settlementId, int userId, CancellationToken cancellationToken = default) => Task.FromResult<Settlement?>(null);
    public Task<IReadOnlyList<SettlementPaymentItem>> GetItemsAsync(int settlementId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SettlementPaymentItem>>([]);
    public Task<IReadOnlyList<SettlementPaymentItem>> GetItemsForRunnerUserAsync(int settlementId, int userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SettlementPaymentItem>>([]);
    public Task<int> InsertSettlementAsync(Settlement settlement, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task InsertSettlementItemAsync(int settlementId, int paymentId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> UpdateStatusAsync(int settlementId, string currentStatus, string targetStatus, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (Existing?.SettlementId != settlementId || Existing.SettlementStatus != currentStatus)
        {
            return Task.FromResult(false);
        }

        Existing.SettlementStatus = targetStatus;
        UpdateCount++;
        return Task.FromResult(true);
    }
}

internal sealed class FakeReportRepository : IReportRepository
{
    public IReadOnlyList<ReportBusinessItem> BusinessItems { get; set; } = [];
    public IReadOnlyList<int> AuditIds { get; set; } = [];
    public ReportRecord? StoredReport { get; private set; }
    public List<int> LinkedAuditIds { get; } = [];

    public Task<IReadOnlyList<ReportMetricRecord>> GetMetricsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ReportMetricRecord>>([]);
    public Task<IReadOnlyList<NodeVolumeRecord>> GetNodeVolumesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<NodeVolumeRecord>>([]);
    public Task<IReadOnlyList<RunnerPerformanceRecord>> GetRunnerPerformanceAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RunnerPerformanceRecord>>([]);
    public Task<IReadOnlyList<ReportRecord>> GetRecentReportsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ReportRecord>>(StoredReport is null ? [] : [StoredReport]);
    public Task<ReportRecord?> GetByIdAsync(int reportId, CancellationToken cancellationToken = default) => Task.FromResult(StoredReport?.ReportId == reportId ? StoredReport : null);
    public Task<IReadOnlyList<ReportBusinessItem>> GetBusinessItemsAsync(string reportType, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default) => Task.FromResult(BusinessItems);
    public Task<IReadOnlyList<int>> GetAuditIdsForReportAsync(string reportType, DateTime periodStart, DateTime periodEnd, IRepositoryTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(AuditIds);
    public Task<IReadOnlyList<ReportAuditItem>> GetReportAuditItemsAsync(int reportId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ReportAuditItem>>([]);

    public Task<int> InsertReportAsync(ReportRecord report, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        report.ReportId = 801;
        report.GeneratedAt = DateTime.Now;
        StoredReport = report;
        return Task.FromResult(report.ReportId);
    }

    public Task InsertReportAuditItemAsync(int reportId, int auditId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default)
    {
        LinkedAuditIds.Add(auditId);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateStatusAsync(int reportId, string reportStatus, CancellationToken cancellationToken = default)
    {
        if (StoredReport?.ReportId != reportId)
        {
            return Task.FromResult(false);
        }

        StoredReport.ReportStatus = reportStatus;
        return Task.FromResult(true);
    }
}
