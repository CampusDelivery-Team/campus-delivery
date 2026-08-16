using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IComplaintRepository
{
    Task<Complaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default);
    Task<bool> CanCreateAsync(int recordId, int publisherUserId, CancellationToken cancellationToken = default);
    Task<Complaint?> GetByIdWithLockAsync(int complaintId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<Complaint?> GetByRecordIdAsync(int recordId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<ComplaintContext?> GetContextByRecordIdAsync(int recordId, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Complaint>> GetAllPagedAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Complaint>> GetByUserIdAsync(int userId, int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> InsertAsync(Complaint complaint, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Complaint complaint, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateRunnerCreditAsync(int runnerId, decimal creditDelta, IRepositoryTransaction transaction, CancellationToken cancellationToken = default);
}
