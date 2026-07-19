namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReviewListItemViewModel
{
    public int ReviewId { get; set; }
    public int RecordId { get; set; }
    public int Rating { get; set; }
    public string RatingDisplayName => new string('★', Rating) + new string('☆', 5 - Rating);
    public string AnonymousDisplayName { get; set; } = string.Empty;
    public string? CommentText { get; set; }
    public DateTime ReviewedAt { get; set; }
    public int CreditDelta { get; set; }
}
