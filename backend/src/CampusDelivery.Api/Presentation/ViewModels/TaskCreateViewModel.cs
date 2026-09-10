using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class TaskCreateViewModel
{
    [Required(ErrorMessage = "请选择任务类型")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择任务类型")]
    [Display(Name = "任务类型")]
    public int? ServiceTypeId { get; set; }

    [Required(ErrorMessage = "请选择收货地址")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择收货地址")]
    [Display(Name = "收货地址")]
    public int? AddressNo { get; set; }

    [Required(ErrorMessage = "请选择交接节点")]
    [Range(1, int.MaxValue, ErrorMessage = "请选择交接节点")]
    [Display(Name = "交接节点")]
    public int? NodeId { get; set; }

    [Required(ErrorMessage = "请填写任务标题")]
    [StringLength(100, ErrorMessage = "任务标题不能超过 100 个字符")]
    [Display(Name = "任务标题")]
    public string TaskTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "请填写附加费，没有附加费时填写 0")]
    [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "附加费必须在 0 到 99999999.99 之间")]
    [Display(Name = "附加费")]
    public decimal? ExtraAmount { get; set; } = 0m;

    [RegularExpression("Y|N", ErrorMessage = "加急标志不合法")]
    public string UrgentFlag { get; set; } = "N";

    [StringLength(100, ErrorMessage = "商家名称不能超过 100 个字符")]
    [Display(Name = "商家名称")]
    public string? MerchantName { get; set; }

    [StringLength(80, ErrorMessage = "平台订单号不能超过 80 个字符")]
    [Display(Name = "平台订单号")]
    public string? PlatformOrderNo { get; set; }

    [StringLength(200, ErrorMessage = "取餐备注不能超过 200 个字符")]
    [Display(Name = "取餐备注")]
    public string? FoodPickupNote { get; set; }

    [StringLength(80, ErrorMessage = "快递公司不能超过 80 个字符")]
    [Display(Name = "快递公司")]
    public string? ExpressCompany { get; set; }

    [StringLength(80, ErrorMessage = "物流单号不能超过 80 个字符")]
    [Display(Name = "物流单号")]
    public string? WaybillNo { get; set; }

    [StringLength(50, ErrorMessage = "取件码不能超过 50 个字符")]
    [Display(Name = "取件码")]
    public string? PickupCode { get; set; }

    [StringLength(200, ErrorMessage = "取件备注不能超过 200 个字符")]
    [Display(Name = "取件备注")]
    public string? ExpressPickupNote { get; set; }

    [StringLength(50, ErrorMessage = "物品类别不能超过 50 个字符")]
    [Display(Name = "物品类别")]
    public string? ItemCategory { get; set; }

    [StringLength(200, ErrorMessage = "取货地点不能超过 200 个字符")]
    [Display(Name = "取货地点")]
    public string? PickupLocation { get; set; }

    [StringLength(200, ErrorMessage = "送达地点不能超过 200 个字符")]
    [Display(Name = "送达地点")]
    public string? DeliveryLocation { get; set; }

    [Display(Name = "期望完成时间")]
    public DateTime? ExpectedFinishAt { get; set; }

    [StringLength(500, ErrorMessage = "任务描述不能超过 500 个字符")]
    [Display(Name = "任务描述")]
    public string? PrivateDescription { get; set; }

    public IReadOnlyList<TaskOptionViewModel> AddressOptions { get; set; } = new List<TaskOptionViewModel>();

    public IReadOnlyList<TaskOptionViewModel> ServiceTypeOptions { get; set; } = new List<TaskOptionViewModel>();

    public IReadOnlyList<TaskOptionViewModel> NodeOptions { get; set; } = new List<TaskOptionViewModel>();
}
