using System.ComponentModel.DataAnnotations;
using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReviewEditViewModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "评价编号无效")]
    public int ReviewId { get; set; }

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

    public static ReviewEditViewModel FromModel(Review review)
    {
        return new ReviewEditViewModel
        {
            ReviewId = review.ReviewId,
            Rating = review.Rating,
            AnonymousFlag = review.AnonymousFlag,
            CommentText = review.CommentText
        };
    }
}
