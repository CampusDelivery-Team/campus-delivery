using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Repositories.Interfaces;
using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace CampusDelivery.Api.Services
{
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<UserService> _logger;

        // 通过构造函数注入用户仓储接口。
        public UserService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public UserAuthenticationState? GetAuthenticationState(int userId)
        {
            User? user = _userRepository.GetUserById(userId);
            return user is null
                ? null
                : new UserAuthenticationState(
                    user.UserId,
                    user.Username,
                    user.UserRole,
                    user.AccountStatus);
        }

        /// <summary>
        /// 核心业务逻辑：用户登录
        /// 返回一个包含三个元素的元组 (是否成功, 错误提示, 用户对象)
        /// </summary>
        public (bool Success, string ErrorMessage, User? User) Login(string username, string password)
        {
            // 1. 去数据库里找这个账号
            var user = _userRepository.GetUserByUsername(username);

            // 2. 账号不存在
            if (user == null)
            {
                return (false, "账号不存在，请先注册", null);
            }

            // 3. 使用 ASP.NET Core PasswordHasher 校验带盐哈希。
            PasswordVerificationResult verificationResult;
            try
            {
                verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (FormatException exception)
            {
                _logger.LogError(
                    exception,
                    "用户 {UserId} 的密码哈希格式无效，需要执行密码迁移或重置。",
                    user.UserId);
                return (false, "该账号的密码数据需要升级，请联系管理员", null);
            }
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return (false, "密码错误，请重新输入", null);
            }

            // 4. 判断账号是否被禁用
            if (user.AccountStatus == AccountStatusCodes.Blocked)
            {
                return (false, "您的账号已被封控，请联系管理员", null);
            }

            if (user.AccountStatus == AccountStatusCodes.Cancelled)
            {
                return (false, "该账号已注销，不能再登录", null);
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _userRepository.UpdatePasswordHash(user.UserId, user.PasswordHash);
            }

            // 5. 校验全部通过，允许登录
            return (true, "登录成功", user);
        }
        /// <summary>
        /// 核心业务逻辑：用户注册
        /// 返回一个包含两个元素的元组 (是否成功, 错误提示)
        /// </summary>
        public UserRegistrationResult Register(
            string username,
            string phone,
            string password)
        {
            // 1. 去数据库查一下，这个账号是不是已经被别人抢注了
            var existingUser = _userRepository.GetUserByUsername(username);
            if (existingUser != null)
            {
                return new(false, "该账号已被注册，请更换一个账号名", UserRegistrationFailure.DuplicateUsername);
            }

            var existingPhoneUser = _userRepository.GetUserByPhone(phone);
            if (existingPhoneUser != null)
            {
                // 如果发现手机号被占用，判断该占用的账号是否已被注销
                if (existingPhoneUser.AccountStatus == AccountStatusCodes.Cancelled)
                {
                    // 为了规避数据库唯一约束，并符合 phone 字段 VARCHAR2(20) 的最大长度限制
                    // 将旧注销账号的手机号加一个废弃后缀释放出来，例如把后三位切掉加上 _del 和 ID：18721960_del41
                    string scrambledPhone = $"{phone.Substring(0, 8)}_del{existingPhoneUser.UserId}";
                    if (scrambledPhone.Length > 20)
                    {
                        scrambledPhone = scrambledPhone.Substring(0, 20);
                    }

                    // 调用现成的 UpdateUserPhone 方法更新旧账号，释放出真实手机号
                    _userRepository.UpdateUserPhone(existingPhoneUser.UserId, scrambledPhone);
                }
                else
                {
                    // 如果账号正常或被封禁，依然阻止注册
                    return new(false, "该手机号已经注册，请更换手机号或直接登录", UserRegistrationFailure.DuplicatePhone);
                }
            }

            // 2. Service 统一生成带盐密码哈希，Repository 只保存哈希结果。
            var user = new User
            {
                Username = username,
                Phone = phone,
                UserRole = "USER",
                AccountStatus = AccountStatusCodes.Normal
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            // 3. 调用持久层，把新用户插进数据库
            UserInsertWriteResult insertResult = _userRepository.InsertUser(user);
            if (insertResult == UserInsertWriteResult.Success)
            {
                return new(true, string.Empty);
            }

            return insertResult switch
            {
                UserInsertWriteResult.DuplicateUsername =>
                    new(false, "该账号已被注册，请更换一个账号名", UserRegistrationFailure.DuplicateUsername),
                UserInsertWriteResult.DuplicatePhone =>
                    new(false, "该手机号已经注册，请更换手机号或直接登录", UserRegistrationFailure.DuplicatePhone),
                _ => new(false, "系统繁忙，注册失败，请稍后再试", UserRegistrationFailure.Unavailable)
            };
        }

        /// <summary>
        /// 规范约束：将数据库的英文角色代码转换为中文展示
        /// </summary>
        public string GetChineseRoleName(string englishRoleCode)
        {
            return englishRoleCode switch
            {
                "USER" => "普通用户",
                "RUNNER" => "跑腿员",
                "ADMIN" => "管理员",
                _ => "未知角色"
            };
        }
        /// <summary>
        /// 更新用户手机号
        /// </summary>
        public UserViewModel? GetProfile(string username)
        {
            User? user = _userRepository.GetUserByUsername(username);
            if (user is null)
            {
                return null;
            }

            UserAddress? address = _userRepository.GetPrimaryAddress(user.UserId);
            return new UserViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Phone = user.Phone,
                UserRole = GetChineseRoleName(user.UserRole),
                RunnerRealName = user.UserRole == "RUNNER"
                    ? _userRepository.GetRunnerRealName(user.UserId)
                    : null,
                CreditScore = user.UserRole == "RUNNER"
                    ? _userRepository.GetRunnerCreditScore(user.UserId)
                    : null,
                HasAddress = address is not null,
                AddressSummary = address is null
                    ? "暂未设置常用地址"
                    : $"{address.Campus} · {address.BuildingRoom}",
                AddressContact = address is null
                    ? "后续可在地址管理中新增收货地址"
                    : $"{address.ContactName} · {address.ContactPhone}"
            };
        }

        public (bool Success, string ErrorMessage) UpdatePhone(string username, string newPhone)
        {
            User? user = _userRepository.GetUserByUsername(username);
            if (user is null)
            {
                return (false, "用户不存在");
            }

            User? phoneOwner = _userRepository.GetUserByPhone(newPhone);
            if (phoneOwner is not null && phoneOwner.UserId != user.UserId)
            {
                if (phoneOwner.AccountStatus == AccountStatusCodes.Cancelled)
                {
                    string scrambledPhone = $"{newPhone.Substring(0, 8)}_del{phoneOwner.UserId}";
                    if (scrambledPhone.Length > 20) scrambledPhone = scrambledPhone.Substring(0, 20);
                    _userRepository.UpdateUserPhone(phoneOwner.UserId, scrambledPhone);
                }
                else
                {
                    return (false, "该手机号已被其他账号使用");
                }
            }

            UserPhoneUpdateWriteResult updateResult = _userRepository.UpdateUserPhone(user.UserId, newPhone);
            if (updateResult == UserPhoneUpdateWriteResult.Success)
            {
                return (true, "");
            }

            if (updateResult == UserPhoneUpdateWriteResult.DuplicatePhone)
            {
                return (false, "该手机号已被其他账号使用");
            }

            return (false, "系统繁忙，更新失败，请稍后再试");
        }

        public (bool Success, string ErrorMessage) CancelOwnAccount(string username)
        {
            User? user = _userRepository.GetUserByUsername(username);
            if (user is null)
            {
                return (false, "用户不存在");
            }

            try
            {
                return _userRepository.UpdateAccountStatus(user.UserId, AccountStatusCodes.Cancelled, AccountStatusCodes.Normal)
                    ? (true, string.Empty)
                    : (false, "账号状态已变化，注销失败，请刷新后重试");
            }
            catch (RepositorySchemaException)
            {
                return (false, "数据库账号状态尚未升级，请联系管理员执行账号状态迁移。");
            }
        }

        public AccountManagementViewModel GetAccountManagement()
        {
            var accounts = _userRepository.GetManagedAccounts();
            return new AccountManagementViewModel
            {
                Accounts = accounts.Select(account => new AccountListItemViewModel
                {
                    UserId = account.UserId,
                    Username = account.Username,
                    Phone = account.Phone,
                    UserRole = account.UserRole,
                    UserRoleDisplayName = GetChineseRoleName(account.UserRole),
                    AccountStatus = account.AccountStatus,
                    AccountStatusDisplayName = DisplayNameService.GetAccountStatusName(account.AccountStatus),
                    RunnerId = account.RunnerId,
                    RealName = account.RealName,
                    RunnerAuditStatus = account.RunnerAuditStatus,
                    RunnerWorkStatus = account.RunnerWorkStatus
                }).ToList(),
                NormalCount = accounts.Count(account => account.AccountStatus == AccountStatusCodes.Normal),
                BlockedCount = accounts.Count(account => account.AccountStatus == AccountStatusCodes.Blocked),
                CancelledCount = accounts.Count(account => account.AccountStatus == AccountStatusCodes.Cancelled)
            };
        }

        public UserAccountOperationResult BlockAccount(int userId) =>
            ExecuteAccountOperation(
                () => _userRepository.UpdateAccountStatus(userId, AccountStatusCodes.Blocked, AccountStatusCodes.Normal),
                "账号已封控",
                "账号无法封控，可能已被处理或不是可管理账号");

        public UserAccountOperationResult UnblockAccount(int userId) =>
            ExecuteAccountOperation(
                () => _userRepository.UpdateAccountStatus(userId, AccountStatusCodes.Normal, AccountStatusCodes.Blocked),
                "账号已解除封控",
                "账号无法解除封控");

        public UserAccountOperationResult CancelAccount(int userId) =>
            ExecuteAccountOperation(
                () => _userRepository.UpdateAccountStatus(userId, AccountStatusCodes.Cancelled, AccountStatusCodes.Normal, AccountStatusCodes.Blocked),
                "账号已注销",
                "账号无法注销，可能已被处理或不是可管理账号");

        public UserAccountOperationResult RevokeRunnerQualification(int userId) =>
            ExecuteAccountOperation(
                () => _userRepository.RevokeRunnerQualification(userId),
                "已撤销跑腿员资格，账号已降为普通用户",
                "无法撤销跑腿员资格");

        private static UserAccountOperationResult ExecuteAccountOperation(
            Func<bool> action,
            string successMessage,
            string failureMessage)
        {
            try
            {
                bool success = action();
                return new UserAccountOperationResult(success, success ? successMessage : failureMessage);
            }
            catch (RepositorySchemaException)
            {
                return new UserAccountOperationResult(
                    false,
                    "数据库账号状态尚未升级，请先执行 003_add_account_lifecycle.sql。");
            }
        }
    }
}
