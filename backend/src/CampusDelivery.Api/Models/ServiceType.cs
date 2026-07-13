namespace CampusDelivery.Api.Models;

public sealed class ServiceType
{
    public int ServiceTypeId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }

    public string? DistanceRule { get; set; }

    public string? UrgentRule { get; set; }

    public string TypeStatus { get; set; } = "ENABLED";
}
