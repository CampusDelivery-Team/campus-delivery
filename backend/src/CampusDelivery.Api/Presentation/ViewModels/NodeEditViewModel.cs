using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class NodeEditViewModel : NodeCreateViewModel
{
    [Required]
    public int NodeId { get; set; }
}
