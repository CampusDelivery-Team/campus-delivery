namespace CampusDelivery.Api.Models;

public sealed class TaskPublishRequest
{
    public int PublisherUserId { get; set; }

    public int ServiceTypeId { get; set; }

    public int AddressNo { get; set; }

    public int NodeId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal TaskPrice { get; set; }

    public string UrgentFlag { get; set; } = "N";

    public string TaskKind { get; set; } = "FOOD";

    public string? MerchantName { get; set; }

    public string? PlatformOrderNo { get; set; }

    public string? FoodPickupNote { get; set; }

    public string? ExpressCompany { get; set; }

    public string? WaybillNo { get; set; }

    public string? PickupCode { get; set; }

    public string? ExpressPickupNote { get; set; }

    public string? ItemCategory { get; set; }

    public string? PickupLocation { get; set; }

    public string? DeliveryLocation { get; set; }

    public DateTime? ExpectedFinishAt { get; set; }

    public string? PrivateDescription { get; set; }
}
