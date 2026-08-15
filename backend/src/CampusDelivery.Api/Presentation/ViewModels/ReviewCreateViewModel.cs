using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public class ReviewCreateViewModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "任务编号无效")]
    public int TaskId { get; set; }

    [Required(ErrorMessage = "请选择评分")]
    [Range(1, 5, ErrorMessage = "评分必须在1到5之间")]
    [Display(Name = "评分")]
    public int? Rating { get; set; }

    [Required]
    [Display(Name = "匿名发布")]
    public char AnonymousFlag { get; set; } = 'N';

    [StringLength(300, ErrorMessage = "评价内容不能超过300字")]
    [Display(Name = "评价内容")]
    public string? CommentText { get; set; }
}
