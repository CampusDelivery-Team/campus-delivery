using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IAuditRepository
{
    Task<IReadOnlyList<AuditLogRecord>> GetRecentAuditsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditTargetRecord>> GetPaymentTargetsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditTargetRecord>> GetRefundTargetsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditTargetRecord>> GetStatusLogTargetsAsync(CancellationToken cancellationToken = default);
    Task<int> GetTargetCountAsync(string auditObject, CancellationToken cancellationToken = default);
    Task<int> InsertAuditLogAsync(AuditLogRecord record, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task InsertAuditLinkAsync(int auditId, string auditObject, int targetId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
}
