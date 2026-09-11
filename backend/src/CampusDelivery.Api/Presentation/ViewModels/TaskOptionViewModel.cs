namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskOptionViewModel
{
    public int Value { get; set; }

    public string Text { get; set; } = string.Empty;

    public decimal? BasePrice { get; set; }

    public string? TaskKind { get; set; }

    public IReadOnlyList<int> AllowedServiceTypeIds { get; set; } = [];
}
