using System;
using System.ComponentModel.DataAnnotations;
using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Presentation.ViewModels;

public class ReviewCreateViewModel
{
    [Required(ErrorMessage = "报告编号不能为空")]
    [Display(Name = "报告编号")]
    public int ReportId { get; set; }

    [Range(1, 5, ErrorMessage = "评分必须在 1 到 5 之间")]
    [Display(Name = "评分")]
    public int? Rating { get; set; }

    [Required(ErrorMessage = "请选择是否匿名")]
    [RegularExpression("Y|N", ErrorMessage = "匿名标志只能是 Y 或 N")]
    [Display(Name = "匿名发布")]
    public char AnonymousFlag { get; set; } = 'N';

    [StringLength(500, ErrorMessage = "评价内容不能超过 500 个字符")]
    [Display(Name = "评价内容")]
    public string? CommentText { get; set; }

    [Display(Name = "信用变动")]
    [Range(-100, 100, ErrorMessage = "信用变动范围必须在 -100 到 100 之间")]
    public int CreditDelta { get; set; }


    public Review ToModel()
    {
        return new Review
        {
            ReportID = ReportId,
            Rating = Rating,
            Anonymous_flag = AnonymousFlag,
            Comment_text = CommentText,
            Reviewed_at = DateTime.Now,
            Credit_delta = CreditDelta
        };
    }
}
