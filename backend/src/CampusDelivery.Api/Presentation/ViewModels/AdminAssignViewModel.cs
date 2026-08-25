namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class AdminAssignViewModel
{
    public List<AdminTaskItemViewModel> WaitingTasks { get; set; } = new();
    public List<AdminReassignableTaskViewModel> ReassignableTasks { get; set; } = new();
    public List<AdminRunnerItemViewModel> AvailableRunners { get; set; } = new();
    public List<AdminRunnerOptionViewModel> ReassignRunners { get; set; } = new();
    public int TaskPageNumber { get; set; }
    public int TaskTotalPages { get; set; }
    public int TaskTotalCount { get; set; }
    public int RunnerPageNumber { get; set; }
    public int RunnerTotalPages { get; set; }
    public int RunnerTotalCount { get; set; }
    public string? ReassignKeyword { get; set; }
    public string? ReassignStatus { get; set; }
    public int ReassignPageNumber { get; set; }
    public int ReassignTotalPages { get; set; }
    public int PageSize { get; set; }
}

public sealed class AdminTaskItemViewModel
{
    public int TaskId { get; set; }
    public int PublisherUserId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal TaskPrice { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string CreatedAddress { get; set; } = string.Empty;
}

public sealed class AdminReassignableTaskViewModel
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskStatusDisplayName { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CurrentRunnerId { get; set; }
    public string CurrentRunnerName { get; set; } = string.Empty;
}

public sealed class AdminRunnerOptionViewModel
{
    public int RunnerId { get; set; }
    public string RealName { get; set; } = string.Empty;
    public int ActiveTaskCount { get; set; }
}

public sealed class AdminRunnerItemViewModel
{
    public int RunnerId { get; set; }
    public int UserId { get; set; }
    public string RealName { get; set; } = string.Empty;
    public decimal CreditScore { get; set; }
    public int ActiveTaskCount { get; set; }
}
