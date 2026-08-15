namespace CampusDelivery.Api.Models;

public sealed class Review
{
    public int ReviewId { get; set; }
    public int TaskId { get; set; }
    public int RecordId { get; set; }
    public int PublisherUserId { get; set; }
    public int Rating { get; set; }
    public char AnonymousFlag { get; set; } = 'N';
    public string? CommentText { get; set; }
    public DateTime ReviewedAt { get; set; }
    public decimal CreditDelta { get; set; }
}
