using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "请输入账号")]
        [Display(Name = "账号")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入密码")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;
    }
}
