using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Services.Interfaces;

public interface IUserService
{
    UserAuthenticationState? GetAuthenticationState(int userId);
    (bool Success, string ErrorMessage, User? User) Login(string username, string password);
    UserRegistrationResult Register(string username, string phone, string password);
    UserViewModel? GetProfile(string username);
    (bool Success, string ErrorMessage) UpdatePhone(string username, string newPhone);
    (bool Success, string ErrorMessage) CancelOwnAccount(string username);
    string GetChineseRoleName(string englishRoleCode);
    AccountManagementViewModel GetAccountManagement();
    UserAccountOperationResult BlockAccount(int userId);
    UserAccountOperationResult UnblockAccount(int userId);
    UserAccountOperationResult CancelAccount(int userId);
    UserAccountOperationResult RevokeRunnerQualification(int userId);
}

public sealed record UserAccountOperationResult(bool Success, string Message);

public sealed record UserAuthenticationState(
    int UserId,
    string Username,
    string UserRole,
    string AccountStatus);
