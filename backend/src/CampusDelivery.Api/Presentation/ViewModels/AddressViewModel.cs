using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class AddressViewModel
{
    public int UserId { get; set; }

    public int AddressNo { get; set; }

    [Required(ErrorMessage = "请填写联系人")]
    [StringLength(50, ErrorMessage = "联系人不能超过 50 个字符")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请填写联系电话")]
    [StringLength(20, ErrorMessage = "联系电话不能超过 20 个字符")]
    [RegularExpression("^[0-9-]{6,20}$", ErrorMessage = "请输入有效的联系电话")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "请填写校区")]
    [StringLength(50, ErrorMessage = "校区不能超过 50 个字符")]
    public string Campus { get; set; } = string.Empty;

    [Required(ErrorMessage = "请填写楼栋与房间")]
    [StringLength(120, ErrorMessage = "楼栋与房间不能超过 120 个字符")]
    public string BuildingRoom { get; set; } = string.Empty;

    public string IsDefault { get; set; } = "N";
}
