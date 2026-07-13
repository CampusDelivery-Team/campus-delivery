namespace CampusDelivery.Api.Models;

public sealed class ManagedAccount
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string UserRole { get; set; } = "USER";
    public string AccountStatus { get; set; } = "NORMAL";
    public int? RunnerId { get; set; }
    public string? RealName { get; set; }
    public string? RunnerAuditStatus { get; set; }
    public string? RunnerWorkStatus { get; set; }
}
