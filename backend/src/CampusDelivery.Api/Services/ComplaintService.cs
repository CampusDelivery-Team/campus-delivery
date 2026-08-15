using CampusDelivery.Api.Models;
using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Services;

public sealed class ComplaintService(
    ComplaintRepository complaintRepository,
    OracleConnectionFactory connectionFactory)
{
    private const int CreditPenalty = 10;

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

    public async Task<bool> CanCreateComplaintAsync(
        int recordId, int currentUserId, CancellationToken cancellationToken = default)
        => await complaintRepository.CanCreateAsync(recordId, currentUserId, cancellationToken);

    public async Task<(bool Success, string Message)> CreateComplaintAsync(
        int recordId, string reason, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return (false, "投诉原因不能为空");
        if (reason.Length > 300) return (false, "投诉原因不能超过300字");
        await using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = (OracleTransaction)(await conn.BeginTransactionAsync(cancellationToken));
        try
        {
            var context = await complaintRepository.GetContextByRecordIdAsync(recordId, conn, tx, cancellationToken);
            if (context == null) { await tx.RollbackAsync(cancellationToken); return (false, "接派记录不存在"); }
            if (context.PublisherUserId != currentUserId) { await tx.RollbackAsync(cancellationToken); return (false, "只能投诉自己发布的订单"); }
            if (context.TaskStatus != "FINISHED") { await tx.RollbackAsync(cancellationToken); return (false, "只有已完成的服务才能投诉"); }

            var existing = await complaintRepository.GetByRecordIdAsync(recordId, conn, tx, cancellationToken);
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
        int complaintId, string decision, string? note, CancellationToken cancellationToken = default)
    {
        if (decision != "UPHELD" && decision != "REJECTED")
            return (false, "无效的处理结果");
        note = note?.Trim();
        if (!string.IsNullOrEmpty(note) && note.Length > 300)
            return (false, "处理说明不能超过300字");
        await using var conn = connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = (OracleTransaction)(await conn.BeginTransactionAsync(cancellationToken));
        try
        {
            var complaint = await complaintRepository.GetByIdWithLockAsync(complaintId, conn, tx, cancellationToken);
            if (complaint == null) { await tx.RollbackAsync(cancellationToken); return (false, "投诉不存在"); }
            if (complaint.ProcessStatus == "DONE") { await tx.RollbackAsync(cancellationToken); return (false, "该投诉已处理"); }

            string resultText = decision == "UPHELD" ? "成立" : "驳回";
            if (!string.IsNullOrEmpty(note)) resultText += "；处理说明：" + note;

            complaint.ProcessStatus = "DONE";
            complaint.ProcessResult = resultText;

            if (!await complaintRepository.UpdateAsync(complaint, conn, tx, cancellationToken))
            { await tx.RollbackAsync(cancellationToken); return (false, "处理投诉失败"); }

            if (decision == "UPHELD")
            {
                var context = await complaintRepository.GetContextByRecordIdAsync(complaint.RecordId, conn, tx, cancellationToken);
                if (context == null) { await tx.RollbackAsync(cancellationToken); return (false, "关联的接派记录不存在"); }
                await DeductCreditAsync(context.RunnerId, conn, tx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return (true, "投诉处理完成");
        }
        catch { await tx.RollbackAsync(cancellationToken); throw; }
    }

    private static async Task DeductCreditAsync(
        int runnerId, OracleConnection conn, OracleTransaction tx, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.BindByName = true;
        cmd.CommandText = "UPDATE APPUSER.runners SET credit_score = GREATEST(0, credit_score - :penalty) WHERE runner_id = :runnerId";
        cmd.Parameters.Add(new OracleParameter("penalty", CreditPenalty));
        cmd.Parameters.Add(new OracleParameter("runnerId", runnerId));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
