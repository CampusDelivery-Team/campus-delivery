namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskHallViewModel
{
    public List<TaskHallItemViewModel> Tasks { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int? ActiveRunnerId { get; set; }
    public string ActiveRunnerStatus { get; set; } = string.Empty;
    public int ActiveTaskCount { get; set; }
}

public sealed class TaskHallItemViewModel
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal TaskPrice { get; set; }
    public string UrgentFlag { get; set; } = "N";
    public string TaskStatus { get; set; } = string.Empty;
    public string TaskStatusDisplayName { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string AddressDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublishedByCurrentUser { get; set; }
}
