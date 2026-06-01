namespace CampusDelivery.Api.Dtos;

public sealed record DatabasePingResponse(
    string Message,
    int UserCount);
