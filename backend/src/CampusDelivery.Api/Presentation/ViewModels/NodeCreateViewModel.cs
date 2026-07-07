using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public class NodeCreateViewModel
{
    [Required(ErrorMessage = "节点类型不能为空")]
    [StringLength(30, ErrorMessage = "节点类型不能超过 30 个字符")]
    [Display(Name = "节点类型")]
    public string NodeType { get; set; } = string.Empty;

    [Required(ErrorMessage = "节点名称不能为空")]
    [StringLength(80, ErrorMessage = "节点名称不能超过 80 个字符")]
    [Display(Name = "节点名称")]
    public string NodeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "位置不能为空")]
    [StringLength(200, ErrorMessage = "位置不能超过 200 个字符")]
    [Display(Name = "位置")]
    public string Location { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "开放时间不能超过 100 个字符")]
    [Display(Name = "开放时间")]
    public string? OpenTime { get; set; }

    [Required(ErrorMessage = "节点状态不能为空")]
    [RegularExpression("NORMAL|CLOSED", ErrorMessage = "节点状态只能是 NORMAL 或 CLOSED")]
    [Display(Name = "节点状态")]
    public string NodeStatus { get; set; } = "NORMAL";
}
