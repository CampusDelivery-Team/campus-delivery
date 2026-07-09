using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "请输入账号")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须在3到50个字符之间")]
        [Display(Name = "账号")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入手机号")]
        [Phone(ErrorMessage = "手机号格式不正确")]
        [Display(Name = "手机号")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入密码")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度至少为6个字符")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "请再次确认密码")]
        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致，请重新输入")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
