using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels
{
    public class AddressViewModel
    {
        // 联合主键 1
        public int UserId { get; set; }

        // 联合主键 2 (隐藏，新增时不需要填，修改时用来定位)
        public int AddressNo { get; set; }

        [Required(ErrorMessage = "请输入联系人姓名")]
        [StringLength(50, ErrorMessage = "联系人姓名不能超过50个字符")]
        [Display(Name = "联系人")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入联系电话")]
        [Phone(ErrorMessage = "联系电话格式不正确")]
        [Display(Name = "联系电话")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入校区 (例如：嘉定校区、四平路校区)")]
        [Display(Name = "校区")]
        public string Campus { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入详细的楼栋和房间号 (例如：F楼302)")]
        [Display(Name = "楼栋房间")]
        public string BuildingRoom { get; set; } = string.Empty;

        // 默认地址标志 (Y/N)，页面上通常用一个 Checkbox(复选框) 来展示
        [Display(Name = "设为默认地址")]
        public string IsDefault { get; set; } = "N";
    }
}
