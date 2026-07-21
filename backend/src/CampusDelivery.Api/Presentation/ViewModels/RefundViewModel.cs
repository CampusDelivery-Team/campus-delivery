using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class RefundCreateViewModel
{
    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public int RecordId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal RefundAmount { get; set; }

    public string PayStatusDisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入退款原因")]
    [StringLength(300, ErrorMessage = "退款原因不能超过 300 个字符")]
    [Display(Name = "退款原因")]
    public string RefundReason { get; set; } = string.Empty;
}

public sealed class RefundSummaryViewModel
{
    public int RefundId { get; set; }

    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal RefundAmount { get; set; }

    public string RefundStatusDisplayName { get; set; } = string.Empty;

    public string? RefundReason { get; set; }

    public string? ReviewReason { get; set; }

    public string? ReviewedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
}

public sealed class RefundAdminListViewModel
{
    public List<RefundSummaryViewModel> Refunds { get; set; } = new();

    public int PageNumber { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }
}

public sealed class RefundReviewViewModel
{
    public int RefundId { get; set; }

    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal RefundAmount { get; set; }

    public string RefundReason { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择审核结果")]
    [RegularExpression("APPROVED|REJECTED", ErrorMessage = "审核结果无效")]
    [Display(Name = "审核结果")]
    public string Decision { get; set; } = "APPROVED";

    [Required(ErrorMessage = "请输入处理原因")]
    [StringLength(300, ErrorMessage = "处理原因不能超过 300 个字符")]
    [Display(Name = "处理原因")]
    public string ReviewReason { get; set; } = string.Empty;
}
