namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class AdminAssignViewModel
{
    public List<AdminTaskItemViewModel> WaitingTasks { get; set; } = new();
    public List<AdminRunnerItemViewModel> FreeRunners { get; set; } = new();
}

public sealed class AdminTaskItemViewModel
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal TaskPrice { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string CreatedAddress { get; set; } = string.Empty;
}

public sealed class AdminRunnerItemViewModel
{
    public int RunnerId { get; set; }
    public string RealName { get; set; } = string.Empty;
    public decimal CreditScore { get; set; }
}



