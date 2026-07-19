using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ReviewEditViewModel : ReviewCreateViewModel
{
    [Range(1, int.MaxValue)]
    public int ReviewId { get; set; }
}
