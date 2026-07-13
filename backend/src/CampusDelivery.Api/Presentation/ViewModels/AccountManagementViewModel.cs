namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class AccountManagementViewModel
{
    public IReadOnlyList<AccountListItemViewModel> Accounts { get; set; } = [];
    public int NormalCount { get; set; }
    public int BlockedCount { get; set; }
    public int CancelledCount { get; set; }
}

public sealed class AccountListItemViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string UserRole { get; set; } = "USER";
    public string UserRoleDisplayName { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = "NORMAL";
    public string AccountStatusDisplayName { get; set; } = string.Empty;
    public int? RunnerId { get; set; }
    public string? RealName { get; set; }
    public string? RunnerAuditStatus { get; set; }
    public string? RunnerWorkStatus { get; set; }
    public bool CanBlock => AccountStatus == "NORMAL";
    public bool CanUnblock => AccountStatus == "BLOCKED";
    public bool CanCancel => AccountStatus is "NORMAL" or "BLOCKED";
    public bool CanRevokeRunner => UserRole == "RUNNER" && AccountStatus != "CANCELLED";
}
