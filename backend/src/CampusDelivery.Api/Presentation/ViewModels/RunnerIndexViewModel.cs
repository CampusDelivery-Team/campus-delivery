namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class RunnerIndexViewModel
{
    public IReadOnlyList<RunnerListItemViewModel> Runners { get; set; } = [];

    public bool PendingOnly { get; set; }

    public int PendingCount { get; set; }

    public int ApprovedCount { get; set; }

    public int AvailableCount { get; set; }
}
