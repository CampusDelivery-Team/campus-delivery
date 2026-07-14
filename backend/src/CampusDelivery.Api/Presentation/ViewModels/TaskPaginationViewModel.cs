namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskPaginationViewModel
{
    public string ActionName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public string PageParameter { get; set; } = "page";
    public string? OtherPageParameter { get; set; }
    public int? OtherPageValue { get; set; }
}
