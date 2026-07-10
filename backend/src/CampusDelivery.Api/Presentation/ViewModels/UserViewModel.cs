using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels
{
    public class UserViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "账号")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入手机号")]
        [Phone(ErrorMessage = "手机号格式不正确")]
        [Display(Name = "手机号")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "角色")]
        public string UserRole { get; set; } = string.Empty;
    }
}
