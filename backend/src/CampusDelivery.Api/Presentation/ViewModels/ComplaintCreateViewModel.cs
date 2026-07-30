using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ComplaintCreateViewModel
{
    [Required]
    public int RecordId { get; set; }

    [Required(ErrorMessage = "请填写投诉原因")]
    [StringLength(300, ErrorMessage = "投诉原因不能超过300字")]
    [Display(Name = "投诉原因")]
    public string Reason { get; set; } = string.Empty;
}
