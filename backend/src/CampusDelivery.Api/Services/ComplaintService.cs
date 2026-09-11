using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;

namespace CampusDelivery.Api.Services;

public sealed class ComplaintService(
    IComplaintRepository complaintRepository,
    IRepositoryTransactionManager transactionManager) : IComplaintService
{
    private const int CreditPenalty = 10;

    public async Task<Complaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default)
        => await complaintRepository.GetByIdAsync(complaintId, cancellationToken);

    public async Task<bool> CanCreateComplaintAsync(
        int recordId,
        int currentUserId,
        CancellationToken cancellationToken = default) =>
        await complaintRepository.CanCreateAsync(recordId, currentUserId, cancellationToken);

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

    public async Task<(IReadOnlyList<Complaint> Items, int TotalCount)> GetMyComplaintsAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 50 ? pageSize : 10;
        int total = await complaintRepository.GetCountByUserIdAsync(userId, cancellationToken);
        int offset = (page - 1) * pageSize;
        var items = await complaintRepository.GetByUserIdAsync(userId, offset, pageSize, cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Complaint> Items, int TotalCount)> GetReceivedComplaintsAsync(
        int runnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is >= 1 and <= 50 ? pageSize : 10;
        int total = await complaintRepository.GetCountByRunnerIdAsync(runnerId, cancellationToken);
        int offset = (page - 1) * pageSize;
        var items = await complaintRepository.GetByRunnerIdPagedAsync(runnerId, offset, pageSize, cancellationToken);
        return (items, total);
    }

    public async Task<(bool Success, string Message)> CreateComplaintAsync(
        int recordId, string reason, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return (false, "投诉原因不能为空");
        if (reason.Length > 300) return (false, "投诉原因不能超过300字");
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            var context = await complaintRepository.GetContextByRecordIdAsync(recordId, transaction, cancellationToken);
            if (context == null) { await transaction.RollbackAsync(cancellationToken); return (false, "接派记录不存在"); }
            if (context.PublisherUserId != currentUserId) { await transaction.RollbackAsync(cancellationToken); return (false, "只能投诉自己发布的订单"); }
            if (context.TaskStatus != "FINISHED") { await transaction.RollbackAsync(cancellationToken); return (false, "只有已完成的服务才能投诉"); }

            var existing = await complaintRepository.GetByRecordIdAsync(recordId, transaction, cancellationToken);
            if (existing != null) { await transaction.RollbackAsync(cancellationToken); return (false, "该接派记录已有投诉"); }

            var complaint = new Complaint { RecordId = recordId, Reason = reason, ProcessStatus = "SUBMITTED" };
            if (!await complaintRepository.InsertAsync(complaint, transaction, cancellationToken))
            { await transaction.RollbackAsync(cancellationToken); return (false, "提交投诉失败"); }
            await transaction.CommitAsync(cancellationToken);
            return (true, "投诉已提交，管理员将尽快处理");
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<(bool Success, string Message)> ProcessComplaintAsync(
        int complaintId, string decision, string? note, CancellationToken cancellationToken = default)
    {
        if (decision != "UPHELD" && decision != "REJECTED")
            return (false, "无效的处理结果");
        note = note?.Trim();
        if (!string.IsNullOrEmpty(note) && note.Length > 300)
            return (false, "处理说明不能超过300字");
        await using IRepositoryTransaction transaction = await transactionManager.BeginAsync(cancellationToken);
        try
        {
            var complaint = await complaintRepository.GetByIdWithLockAsync(complaintId, transaction, cancellationToken);
            if (complaint == null) { await transaction.RollbackAsync(cancellationToken); return (false, "投诉不存在"); }
            if (complaint.ProcessStatus == "DONE") { await transaction.RollbackAsync(cancellationToken); return (false, "该投诉已处理"); }

            string resultText = decision == "UPHELD" ? "成立" : "驳回";
            if (!string.IsNullOrEmpty(note)) resultText += "；处理说明：" + note;

            complaint.ProcessStatus = "DONE";
            complaint.ProcessResult = resultText;

            if (!await complaintRepository.UpdateAsync(complaint, transaction, cancellationToken))
            { await transaction.RollbackAsync(cancellationToken); return (false, "处理投诉失败"); }

            if (decision == "UPHELD")
            {
                var context = await complaintRepository.GetContextByRecordIdAsync(complaint.RecordId, transaction, cancellationToken);
                if (context == null) { await transaction.RollbackAsync(cancellationToken); return (false, "关联的接派记录不存在"); }
                await complaintRepository.UpdateRunnerCreditAsync(
                    context.RunnerId,
                    -CreditPenalty,
                    transaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "投诉处理完成");
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }
}
