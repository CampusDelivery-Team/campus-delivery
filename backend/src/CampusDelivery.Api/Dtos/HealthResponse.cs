namespace CampusDelivery.Api.Dtos;

public sealed record HealthResponse(
    string Application,
    string Status,
    string Environment,
    DateTimeOffset ServerTime);
