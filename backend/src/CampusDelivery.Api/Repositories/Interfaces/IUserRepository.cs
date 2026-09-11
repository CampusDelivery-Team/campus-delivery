using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Repositories.Interfaces;

public interface IUserRepository
{
    User? GetUserById(int userId);
    User? GetUserByUsername(string username);
    User? GetUserByPhone(string phone);
    string? GetRunnerRealName(int userId);
    decimal? GetRunnerCreditScore(int userId);
    UserInsertWriteResult InsertUser(User user);
    bool UpdatePasswordHash(int userId, string passwordHash);
    UserPhoneUpdateWriteResult UpdateUserPhone(int userId, string newPhone);
    UserAddress? GetPrimaryAddress(int userId);
    List<ManagedAccount> GetManagedAccounts();
    bool UpdateAccountStatus(int userId, string nextStatus, params string[] allowedCurrentStatuses);
    bool RevokeRunnerQualification(int userId);
}
