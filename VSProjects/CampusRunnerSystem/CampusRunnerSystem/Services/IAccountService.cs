using CampusRunnerSystem.Models;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public interface IAccountService
{
    Result<LoginUserViewModel> Login(string username, string password);
}
