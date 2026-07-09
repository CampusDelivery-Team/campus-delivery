namespace CampusDelivery.Api.Models
{
    public class UserAddress
    {
        // 所属用户编号 (联合主键)
        public int UserId { get; set; }

        // 地址序号 (联合主键)
        public int AddressNo { get; set; }

        // 联系人
        public string ContactName { get; set; } = string.Empty;

        // 联系电话
        public string ContactPhone { get; set; } = string.Empty;

        // 校区
        public string Campus { get; set; } = string.Empty;

        // 楼栋房间
        public string BuildingRoom { get; set; } = string.Empty;

        // 默认地址标志：Y/N
        public string IsDefault { get; set; } = "N";
    }
}
