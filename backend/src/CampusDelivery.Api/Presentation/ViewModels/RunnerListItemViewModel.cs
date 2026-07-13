namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class RunnerListItemViewModel
{
    public int RunnerId { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;

    public string IdentityInfo { get; set; } = string.Empty;

    public string AuditStatus { get; set; } = "PENDING";

    public string AuditStatusDisplayName { get; set; } = "待审核";

    public string WorkStatus { get; set; } = "OFFLINE";

    public string WorkStatusDisplayName { get; set; } = "离线";

    public decimal CreditScore { get; set; }

    public bool CanReview => AuditStatus == "PENDING";

    public bool CanChangeAvailability => AuditStatus == "APPROVED" && WorkStatus != "BUSY";
}
