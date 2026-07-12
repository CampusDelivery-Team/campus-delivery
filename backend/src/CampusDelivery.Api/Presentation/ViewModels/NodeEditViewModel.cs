using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class NodeEditViewModel : NodeCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "节点编号无效")]
    public int NodeId { get; set; }
}
