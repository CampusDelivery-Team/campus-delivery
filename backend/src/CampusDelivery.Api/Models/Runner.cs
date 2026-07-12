namespace CampusDelivery.Api.Models;

public sealed class Runner
{
    public int RunnerId { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string UserRole { get; set; } = "USER";

    public string AccountStatus { get; set; } = "NORMAL";

    public string RealName { get; set; } = string.Empty;

    public string IdentityInfo { get; set; } = string.Empty;

    public string AuditStatus { get; set; } = "PENDING";

    public string WorkStatus { get; set; } = "OFFLINE";

    public decimal CreditScore { get; set; } = 100;
}
