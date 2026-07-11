using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class RunnerApplicationFormViewModel
{
    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(50, ErrorMessage = "真实姓名不能超过 50 个字符")]
    [Display(Name = "真实姓名")]
    public string RealName { get; set; } = string.Empty;

    [Required(ErrorMessage = "身份信息不能为空")]
    [StringLength(200, ErrorMessage = "身份信息不能超过 200 个字符")]
    [Display(Name = "身份信息")]
    public string IdentityInfo { get; set; } = string.Empty;
}
