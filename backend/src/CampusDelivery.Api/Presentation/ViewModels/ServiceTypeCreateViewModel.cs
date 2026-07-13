using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public class ServiceTypeCreateViewModel
{
    [Required(ErrorMessage = "服务名称不能为空")]
    [StringLength(50, ErrorMessage = "服务名称不能超过 50 个字符")]
    [Display(Name = "服务名称")]
    public string ServiceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "基础价格不能为空")]
    [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "基础价格必须在 0 到 99999999.99 之间")]
    [Display(Name = "基础价格")]
    public decimal? BasePrice { get; set; }

    [StringLength(200, ErrorMessage = "距离规则不能超过 200 个字符")]
    [Display(Name = "距离规则")]
    public string? DistanceRule { get; set; }

    [StringLength(200, ErrorMessage = "加急规则不能超过 200 个字符")]
    [Display(Name = "加急规则")]
    public string? UrgentRule { get; set; }

    [Required(ErrorMessage = "服务状态不能为空")]
    [RegularExpression("ENABLED|DISABLED", ErrorMessage = "服务状态只能是 ENABLED 或 DISABLED")]
    [Display(Name = "服务状态")]
    public string TypeStatus { get; set; } = string.Empty;
}
