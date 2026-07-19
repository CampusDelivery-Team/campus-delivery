namespace CampusDelivery.Api.Models;

public sealed class Review
{
    public int ReviewId { get; set; }
    public int RecordId { get; set; }
    public int Rating { get; set; }
    public string AnonymousFlag { get; set; } = "N";
    public string? CommentText { get; set; }
    public DateTime ReviewedAt { get; set; }
    public int CreditDelta { get; set; }
}
