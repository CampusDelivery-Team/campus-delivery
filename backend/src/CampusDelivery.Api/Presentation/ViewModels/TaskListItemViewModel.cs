namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskListItemViewModel
{
    public int TaskId { get; set; }

    public string TaskKindDisplayName { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string TaskTitle { get; set; } = string.Empty;

    public decimal TaskPrice { get; set; }

    public string UrgentFlagDisplayName { get; set; } = string.Empty;

    public string TaskStatusDisplayName { get; set; } = string.Empty;

    public string AddressSummary { get; set; } = string.Empty;

    public string NodeName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool CanCancel { get; set; }
}
