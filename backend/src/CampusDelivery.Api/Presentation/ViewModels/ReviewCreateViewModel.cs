using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public class ReviewCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "接派记录编号无效")]
    public int RecordId { get; set; }

    [Range(1, 5, ErrorMessage = "评分必须在 1 到 5 星之间")]
    [Display(Name = "服务评分")]
    public int Rating { get; set; } = 5;

    [Required]
    [RegularExpression("Y|N", ErrorMessage = "匿名标识无效")]
    [Display(Name = "发布方式")]
    public string AnonymousFlag { get; set; } = "N";

    [StringLength(300, ErrorMessage = "评价内容不能超过 300 个字符")]
    [Display(Name = "评价内容")]
    public string? CommentText { get; set; }
}
