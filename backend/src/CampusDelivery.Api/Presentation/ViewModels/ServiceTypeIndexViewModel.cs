namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ServiceTypeIndexViewModel
{
    public IReadOnlyList<ServiceTypeListItemViewModel> ServiceTypes { get; set; } = [];

    public ServiceTypeCreateViewModel CreateModel { get; set; } = new();

    public ServiceTypeEditViewModel EditModel { get; set; } = new();
}
