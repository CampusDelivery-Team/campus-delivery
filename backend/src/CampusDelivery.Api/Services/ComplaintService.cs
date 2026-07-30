using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class ComplaintService(
    ComplaintRepository complaintRepository,
    OracleConnectionFactory connectionFactory)
{
    public async Task<Complaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default)
        => await complaintRepository.GetByIdAsync(complaintId, cancellationToken);

    public async Task<(IReadOnlyList<Complaint> Items, int TotalCount)> GetAllPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 50 ? pageSize : 20;
        int total = await complaintRepository.GetTotalCountAsync(cancellationToken);
        int offset = (page - 1) * pageSize;
        var items = await complaintRepository.GetAllPagedAsync(offset, pageSize, cancellationToken);
        return (items, total);
    }

    public async Task<(bool Success, string Message)> CreateComplaintAsync(
        int recordId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return (false, "投诉原因不能为空");
        if (reason.Length > 300) return (false, "投诉原因不能超过300字");
        await using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = (OracleTransaction)(await conn.BeginTransactionAsync(cancellationToken));
        try
        {
            var existing = await complaintRepository.GetByRecordIdAsync(recordId, cancellationToken);
            if (existing != null) { await tx.RollbackAsync(cancellationToken); return (false, "该接派记录已有投诉"); }
            var complaint = new Complaint { RecordId = recordId, Reason = reason, ProcessStatus = "SUBMITTED" };
            if (!await complaintRepository.InsertAsync(complaint, conn, tx, cancellationToken))
            { await tx.RollbackAsync(cancellationToken); return (false, "提交投诉失败"); }
            await tx.CommitAsync(cancellationToken);
            return (true, "投诉已提交，管理员将尽快处理");
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<(bool Success, string Message)> ProcessComplaintAsync(
        int complaintId, string processStatus, string? processResult,
        CancellationToken cancellationToken = default)
    {
        if (processStatus != "PROCESSING" && processStatus != "DONE")
            return (false, "无效的处理状态");
        await using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = (OracleTransaction)(await conn.BeginTransactionAsync(cancellationToken));
        try
        {
            var complaint = await complaintRepository.GetByIdAsync(complaintId, cancellationToken);
            if (complaint == null) { await tx.RollbackAsync(cancellationToken); return (false, "投诉不存在"); }
            complaint.ProcessStatus = processStatus;
            complaint.ProcessResult = processResult;
            if (!await complaintRepository.UpdateAsync(complaint, conn, tx, cancellationToken))
            { await tx.RollbackAsync(cancellationToken); return (false, "处理投诉失败"); }
            await tx.CommitAsync(cancellationToken);
            return (true, processStatus == "DONE" ? "投诉处理完成" : "投诉处理中");
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }
}
