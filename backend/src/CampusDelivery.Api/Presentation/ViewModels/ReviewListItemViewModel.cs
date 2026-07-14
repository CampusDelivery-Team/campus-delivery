using System;
using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReviewListItemViewModel
{
    public int ReviewId { get; set; }

    public int? ReportId { get; set; }

    public int? Rating { get; set; }

    [Display(Name = "评分")]
    public string RatingDisplayName => Rating.HasValue
        ? Rating.Value switch
        {
            1 => "⭐ 很差",
            2 => "⭐⭐ 差",
            3 => "⭐⭐⭐ 一般",
            4 => "⭐⭐⭐⭐ 好",
            5 => "⭐⭐⭐⭐⭐ 很好",
            _ => $"{Rating} 星"
        }
        : "未评分";

    [Display(Name = "匿名标志")]
    public char AnonymousFlag { get; set; } = 'N';

    [Display(Name = "是否匿名")]
    public string AnonymousDisplayName => AnonymousFlag == 'Y' ? "匿名" : "实名";

    [Display(Name = "评价内容")]
    public string? CommentText { get; set; }

    [Display(Name = "评价时间")]
    public DateTime ReviewedAt { get; set; }

    [Display(Name = "信用变动")]
    public int CreditDelta { get; set; }

    [Display(Name = "信用变动(带符号)")]
    public string CreditDeltaDisplayName => CreditDelta >= 0 ? $"+{CreditDelta}" : $"{CreditDelta}";

    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
