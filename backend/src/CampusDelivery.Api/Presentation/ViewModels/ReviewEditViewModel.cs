using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReviewEditViewModel : ReviewCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "评价编号无效")]
    [Display(Name = "评价编号")]
    public int ReviewId { get; set; }

    public static ReviewEditViewModel FromModel(Review review)
    {
        return new ReviewEditViewModel
        {
            ReviewId = review.ReviewID,
            ReportId = review.ReportID ?? 0,
            Rating = review.Rating,
            AnonymousFlag = review.Anonymous_flag,
            CommentText = review.Comment_text,
            CreditDelta = review.Credit_delta
        };
    }


    public Review ToModel(Review existing)
    {
        existing.Rating = Rating;
        existing.Anonymous_flag = AnonymousFlag;
        existing.Comment_text = CommentText;
        existing.Credit_delta = CreditDelta;
        return existing;
    }
}
