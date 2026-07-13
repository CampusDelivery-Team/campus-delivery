namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ServiceNodeRuleIndexViewModel
{
    public IReadOnlyList<ServiceNodeRuleListItemViewModel> Rules { get; set; } = [];

    public IReadOnlyList<ServiceNodeRuleOptionViewModel> ServiceTypes { get; set; } = [];

    public IReadOnlyList<ServiceNodeRuleOptionViewModel> Nodes { get; set; } = [];

    public ServiceNodeRuleCreateViewModel CreateModel { get; set; } = new();

    public int ServiceTypeCount => Rules.Select(rule => rule.ServiceTypeId).Distinct().Count();

    public int NodeCount => Rules.Select(rule => rule.NodeId).Distinct().Count();
}
