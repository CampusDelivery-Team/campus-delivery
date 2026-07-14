namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class MyTasksViewModel
{
    public List<MyTaskItemViewModel> ActiveTasks { get; set; } = new();
    public int RunnerId { get; set; }
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
}

public sealed class MyTaskItemViewModel
{
    public int TaskId { get; set; }
    public int RecordId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal TaskPrice { get; set; }
    public string UrgentFlag { get; set; } = "N";
    public string TaskStatus { get; set; } = string.Empty;
    public string TaskStatusDisplayName { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string AddressDisplay { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public bool ReceiptConfirmed { get; set; }
    public List<TaskStatusLogViewModel> Logs { get; set; } = new();
}

public sealed class TaskStatusLogViewModel
{
    public string? StatusBeforeDisplayName { get; set; }
    public string StatusAfterDisplayName { get; set; } = string.Empty;
    public string? ActionName { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
}

public sealed class ReceiptTasksViewModel
{
    public List<ReceiptTaskItemViewModel> Tasks { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
}

public sealed class ReceiptTaskItemViewModel
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal TaskPrice { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
}
