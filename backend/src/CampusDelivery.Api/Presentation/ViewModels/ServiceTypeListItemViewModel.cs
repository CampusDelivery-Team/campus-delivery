namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ServiceTypeListItemViewModel
{
    public int ServiceTypeId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }

    public string? DistanceRule { get; set; }

    public string? UrgentRule { get; set; }

    public string TypeStatus { get; set; } = "ENABLED";

    public string TypeStatusDisplayName { get; set; } = "启用";
}
