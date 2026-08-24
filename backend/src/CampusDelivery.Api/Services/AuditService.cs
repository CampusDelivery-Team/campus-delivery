using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class AuditService(
    IAuditRepository auditRepository,
    IRepositoryTransactionManager transactionManager) : IAuditService
{
    public async Task<AuditIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AuditLogRecord> logs = await auditRepository.GetRecentAuditsAsync(cancellationToken);
        return new AuditIndexViewModel
        {
            Logs = logs.Select(record => AuditLogItemViewModel.FromModel(
                record,
                DisplayNameService.GetAuditObjectName(record.AuditObject),
                DisplayNameService.GetAuditResultName(record.AuditResult))).ToList(),
            PaymentTargetCount = await auditRepository.GetTargetCountAsync("PAYMENT", cancellationToken),
            RefundTargetCount = await auditRepository.GetTargetCountAsync("REFUND", cancellationToken),
            StatusLogTargetCount = await auditRepository.GetTargetCountAsync("LOG", cancellationToken)
        };
    }

    public async Task<AuditCreateViewModel> BuildCreateModelAsync(
        string auditObject,
        CancellationToken cancellationToken = default)
    {
        auditObject = NormalizeAuditObject(auditObject);
        IReadOnlyList<AuditTargetRecord> targets = auditObject switch
        {
            "PAYMENT" => await auditRepository.GetPaymentTargetsAsync(cancellationToken),
            "REFUND" => await auditRepository.GetRefundTargetsAsync(cancellationToken),
            "LOG" => await auditRepository.GetStatusLogTargetsAsync(cancellationToken),
            _ => Array.Empty<AuditTargetRecord>()
        };

        return new AuditCreateViewModel
        {
            AuditObject = auditObject,
            AuditObjectDisplayName = DisplayNameService.GetAuditObjectName(auditObject),
            AuditResult = "PASS",
            Targets = targets.Select(AuditTargetViewModel.FromModel).ToList()
        };
    }

    public async Task<AuditOperationResult> CreateAuditAsync(
        AuditCreateViewModel model,
        CancellationToken cancellationToken = default)
    {
        string auditObject = NormalizeAuditObject(model.AuditObject);
        if (auditObject is not ("PAYMENT" or "REFUND" or "LOG"))
        {
            return new AuditOperationResult(false, "审计对象无效。");
        }

        if (model.AuditResult is not ("PASS" or "ABNORMAL"))
        {
            return new AuditOperationResult(false, "审计结果无效。");
        }

        var targetIds = model.TargetIds.Distinct().Where(id => id > 0).ToList();
        if (targetIds.Count == 0)
        {
            return new AuditOperationResult(false, "请至少选择一条审计对象。");
        }

        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);

        try
        {
            int auditId = await auditRepository.InsertAuditLogAsync(new AuditLogRecord
            {
                AuditObject = auditObject,
                AuditResult = model.AuditResult,
                ExceptionNote = string.IsNullOrWhiteSpace(model.ExceptionNote) ? null : model.ExceptionNote.Trim()
            }, transaction, cancellationToken);

            foreach (int targetId in targetIds)
            {
                await auditRepository.InsertAuditLinkAsync(
                    auditId,
                    auditObject,
                    targetId,
                    transaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new AuditOperationResult(true, "审计记录已保存。");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string NormalizeAuditObject(string? auditObject)
    {
        return string.IsNullOrWhiteSpace(auditObject) ? "PAYMENT" : auditObject.Trim().ToUpperInvariant();
    }
}

