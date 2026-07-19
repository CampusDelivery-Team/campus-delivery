namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskIndexViewModel
{
    public bool IsAdminView { get; set; }

    public IReadOnlyList<TaskListItemViewModel> Tasks { get; set; } = new List<TaskListItemViewModel>();
}
