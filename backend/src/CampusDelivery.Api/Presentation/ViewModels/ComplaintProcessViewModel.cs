using CampusDelivery.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ComplaintProcessViewModel
{
    public int ComplaintId { get; set; }
    public int RecordId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = "SUBMITTED";

    [Required(ErrorMessage = "请选择处理结果")]
    [Display(Name = "处理结果")]
    public string Decision { get; set; } = "UPHELD";

    [StringLength(300, ErrorMessage = "处理说明不能超过300字")]
    [Display(Name = "处理说明")]
    public string? ProcessNote { get; set; }

    public static ComplaintProcessViewModel FromModel(Complaint complaint)
    {
        return new ComplaintProcessViewModel
        {
            ComplaintId = complaint.ComplaintId,
            RecordId = complaint.RecordId,
            Reason = complaint.Reason,
            CurrentStatus = complaint.ProcessStatus,
            Decision = "UPHELD"
        };
    }
}
