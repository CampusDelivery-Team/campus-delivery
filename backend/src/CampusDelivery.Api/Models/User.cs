namespace CampusDelivery.Api.Models
{
    public class User
    {
        // 用户编号，主键
        public int UserId { get; set; }

        // 账号
        public string Username { get; set; } = string.Empty;

        // 手机号
        public string Phone { get; set; } = string.Empty;

        // 密码散列值
        public string PasswordHash { get; set; } = string.Empty;

        // 用户角色：USER/RUNNER/ADMIN
        public string UserRole { get; set; } = "USER";

        // 账号状态：NORMAL/DISABLED
        public string AccountStatus { get; set; } = "NORMAL";
    }
}
