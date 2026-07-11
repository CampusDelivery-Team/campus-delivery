using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ServiceTypeEditViewModel : ServiceTypeCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "服务类型编号无效")]
    public int ServiceTypeId { get; set; }
}
