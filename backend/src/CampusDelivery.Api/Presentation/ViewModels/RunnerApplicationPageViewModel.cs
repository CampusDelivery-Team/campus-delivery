namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class RunnerApplicationPageViewModel
{
    public bool HasApplication { get; set; }

    public bool CanSubmit { get; set; }

    public bool IsResubmission { get; set; }

    public string AuditStatus { get; set; } = string.Empty;

    public string AuditStatusDisplayName { get; set; } = string.Empty;

    public string WorkStatus { get; set; } = string.Empty;

    public string WorkStatusDisplayName { get; set; } = string.Empty;

    public RunnerApplicationFormViewModel Form { get; set; } = new();
}
