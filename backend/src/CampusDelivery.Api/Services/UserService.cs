using CampusDelivery.Api.Models;
using CampusDelivery.Api.Repositories;

namespace CampusDelivery.Api.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepository;

        // 通过构造函数注入，拿到UserRepository 去查数据库
        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
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

            // 3. 密码比对
            // (注意：根据 002_init_base_data.sql 脚本，目前测试账号密码暂存的是 '123456' 明文。后期如果接入了加密算法，这里应该比对 Hash 值)
            if (user.PasswordHash != password)
            {
                return (false, "密码错误，请重新输入", null);
            }

            // 4. 判断账号是否被禁用
            if (user.AccountStatus == "DISABLED")
            {
                return (false, "您的账号已被禁用，请联系管理员", null);
            }

            // 5. 校验全部通过，允许登录
            return (true, "登录成功", user);
        }
        /// <summary>
        /// 核心业务逻辑：用户注册
        /// 返回一个包含两个元素的元组 (是否成功, 错误提示)
        /// </summary>
        public (bool Success, string ErrorMessage) Register(User user)
        {
            // 1. 去数据库查一下，这个账号是不是已经被别人抢注了
            var existingUser = _userRepository.GetUserByUsername(user.Username);
            if (existingUser != null)
            {
                return (false, "该账号已被注册，请更换一个账号名");
            }

            // 2. 调用持久层，把新用户插进数据库
            bool isInserted = _userRepository.InsertUser(user);
            if (isInserted)
            {
                return (true, ""); // 成功，没有错误信息
            }

            return (false, "系统繁忙，注册失败，请稍后再试");
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
        public (bool Success, string ErrorMessage) UpdatePhone(int userId, string newPhone)
        {
            bool isUpdated = _userRepository.UpdateUserPhone(userId, newPhone);
            if (isUpdated)
            {
                return (true, "");
            }
            return (false, "系统繁忙，更新失败，请稍后再试");
        }
    }
}
