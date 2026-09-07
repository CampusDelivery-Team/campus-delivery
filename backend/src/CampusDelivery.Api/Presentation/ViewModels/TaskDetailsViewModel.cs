namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskDetailsViewModel
{
    public int TaskId { get; set; }
    public int? RecordId { get; set; }
    public bool CanReview { get; set; }
    public bool HasReview { get; set; }
    public bool CanComplain { get; set; }
    public bool CanCancel { get; set; }
    public string? RunnerRealName { get; set; }
    public decimal? RunnerCreditScore { get; set; }
    public string? RunnerWorkStatus { get; set; }
    public string PublisherUsername { get; set; } = string.Empty;
    public string TaskKindDisplayName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string TaskTitle { get; set; } = string.Empty;
    public decimal TaskPrice { get; set; }
    public string UrgentFlagDisplayName { get; set; } = string.Empty;
    public string TaskStatusDisplayName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string AddressSummary { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<TaskDetailFieldViewModel> DetailFields { get; set; } = new();
}

public sealed class TaskDetailFieldViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
