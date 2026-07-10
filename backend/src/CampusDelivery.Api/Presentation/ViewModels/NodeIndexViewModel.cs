namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class NodeIndexViewModel
{
    public IReadOnlyList<NodeListItemViewModel> Nodes { get; set; } = [];

    public bool IsReadOnly { get; set; } = true;
}
