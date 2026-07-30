using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class PaymentConfirmViewModel
{
    public int TaskId { get; set; }

    public int RecordId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal TaskAmount { get; set; }

    public string TaskStatusDisplayName { get; set; } = string.Empty;

    public bool ReceiptConfirmed { get; set; }

    public bool CanSubmitPayment { get; set; }

    [Required(ErrorMessage = "请选择支付方式")]
    [RegularExpression("WECHAT|ALIPAY|CASH", ErrorMessage = "支付方式无效")]
    [Display(Name = "支付方式")]
    public string PayMethod { get; set; } = "WECHAT";

    [StringLength(50, ErrorMessage = "第三方流水号不能超过 50 个字符")]
    [Display(Name = "第三方流水号")]
    public string? ThirdTradeNo { get; set; }
}

public sealed class PaymentStatusQueryViewModel
{
    public int? TaskId { get; set; }

    public int? PaymentId { get; set; }

    public string? QueryText { get; set; }

    public PaymentSummaryViewModel? Payment { get; set; }

    public IReadOnlyList<PaymentSummaryViewModel> Payments { get; set; } = Array.Empty<PaymentSummaryViewModel>();

    public int PageNumber { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }
}

public sealed class PaymentSummaryViewModel
{
    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public int RecordId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal OrderAmount { get; set; }

    public decimal PayAmount { get; set; }

    public string PayMethodDisplayName { get; set; } = string.Empty;

    public string PayStatusDisplayName { get; set; } = string.Empty;

    public string? ThirdTradeNo { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class PaymentStatusViewModel
{
    public List<PaymentSummaryViewModel> Payments { get; set; } = new();

    public int PageNumber { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }
}
