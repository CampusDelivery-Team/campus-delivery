using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReviewListItemViewModel
{
    public int ReviewId { get; set; }
    public int RecordId { get; set; }
    public int Rating { get; set; }
    public char AnonymousFlag { get; set; }
    public string? CommentText { get; set; }
    public DateTime ReviewedAt { get; set; }
    public int CreditDelta { get; set; }

    public string RatingDisplayName => Rating switch
    {
        1 => "很 差",
        2 => "较 差",
        3 => "一 般",
        4 => "较 好",
        5 => "很 好",
        _ => $"{Rating} 星"  
    };

    public string AnonymousDisplayName => AnonymousFlag == 'Y' ? "匿名" : "实名";

    public string CreditDeltaDisplayName => CreditDelta >= 0 ? $"+{CreditDelta}" : $"{CreditDelta}";

    public static ReviewListItemViewModel FromModel(Review review)
    {
        return new ReviewListItemViewModel
        {
            ReviewId = review.ReviewId,
            RecordId = review.RecordId,
            Rating = review.Rating,
            AnonymousFlag = review.AnonymousFlag,
            CommentText = review.CommentText,
            ReviewedAt = review.ReviewedAt,
            CreditDelta = review.CreditDelta
        };
    }
}
