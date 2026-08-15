using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReviewEditViewModel : ReviewCreateViewModel
{
    public int ReviewId { get; set; }

    public static ReviewEditViewModel FromModel(Review review)
    {
        return new ReviewEditViewModel
        {
            ReviewId = review.ReviewId,
            RecordId = review.RecordId,
            Rating = review.Rating,
            AnonymousFlag = review.AnonymousFlag,
            CommentText = review.CommentText
        };
    }
}
