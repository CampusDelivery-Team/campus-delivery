namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class NodeIndexViewModel
{
    public IReadOnlyList<NodeListItemViewModel> Nodes { get; set; } = [];

    public NodeCreateViewModel CreateModel { get; set; } = new();

    public NodeEditViewModel EditModel { get; set; } = new();
}
