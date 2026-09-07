namespace CampusDelivery.Api.Models;

public sealed class TaskDetailsRecord
{
    public TaskRecord Task { get; set; } = new();
    public int? RecordId { get; set; }
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
    public string? RunnerRealName { get; set; }
    public decimal? RunnerCreditScore { get; set; }
    public string? RunnerWorkStatus { get; set; }
}
