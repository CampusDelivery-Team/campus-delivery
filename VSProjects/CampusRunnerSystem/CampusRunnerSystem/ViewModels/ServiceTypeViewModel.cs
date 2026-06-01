namespace CampusRunnerSystem.ViewModels;

public class ServiceTypeViewModel
{
    public int ServiceTypeId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string DistanceRule { get; set; } = string.Empty;
    public string UrgentRule { get; set; } = string.Empty;
    public string TypeStatus { get; set; } = string.Empty;
}
