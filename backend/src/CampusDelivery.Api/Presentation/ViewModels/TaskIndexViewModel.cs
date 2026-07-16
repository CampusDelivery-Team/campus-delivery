namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskIndexViewModel
{
    public IReadOnlyList<TaskListItemViewModel> Tasks { get; set; } = new List<TaskListItemViewModel>();
}
