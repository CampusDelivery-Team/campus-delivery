using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IComplaintService
{
    Task<Complaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default);
    Task<bool> CanCreateComplaintAsync(int recordId, int currentUserId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Complaint> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Complaint> Items, int TotalCount)> GetMyComplaintsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateComplaintAsync(int recordId, string reason, int currentUserId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ProcessComplaintAsync(int complaintId, string decision, string? note, CancellationToken cancellationToken = default);
}
