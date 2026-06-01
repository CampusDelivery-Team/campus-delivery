using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Repositories;

public interface IAccountRepository
{
    LoginUserViewModel? FindByUsername(string username);
}
