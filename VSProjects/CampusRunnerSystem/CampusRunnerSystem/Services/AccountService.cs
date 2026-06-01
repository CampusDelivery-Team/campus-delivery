using CampusRunnerSystem.Models;
using CampusRunnerSystem.Repositories;
using CampusRunnerSystem.ViewModels;

namespace CampusRunnerSystem.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public Result<LoginUserViewModel> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Result<LoginUserViewModel>.Fail("请输入用户名和密码。");
        }

        var user = _accountRepository.FindByUsername(username.Trim());
        if (user == null)
        {
            return Result<LoginUserViewModel>.Fail("用户名或密码错误。");
        }

        if (user.AccountStatus != SystemConstants.AccountStatus.Normal)
        {
            return Result<LoginUserViewModel>.Fail("账号已被禁用，请联系管理员。");
        }

        // TODO: 后续改为哈希验证。
        if (user.PasswordHash != password)
        {
            return Result<LoginUserViewModel>.Fail("用户名或密码错误。");
        }

        return Result<LoginUserViewModel>.Ok(user, "登录成功。");
    }
}
