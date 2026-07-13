using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ServiceNodeRuleCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "请选择服务类型")]
    public int ServiceTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "请选择适用节点")]
    public int NodeId { get; set; }
}
