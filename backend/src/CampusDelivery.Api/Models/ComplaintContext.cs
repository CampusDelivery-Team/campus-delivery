namespace CampusDelivery.Api.Models;

public sealed record ComplaintContext(
    int TaskId,
    int RunnerId,
    int PublisherUserId,
    string TaskStatus,
    string TaskTitle);
