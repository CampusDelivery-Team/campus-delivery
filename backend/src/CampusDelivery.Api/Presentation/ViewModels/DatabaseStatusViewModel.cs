namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class DatabaseStatusViewModel
{
    public bool IsConnected { get; set; }

    public int? UserCount { get; set; }

    public string Message { get; set; } = string.Empty;
}
