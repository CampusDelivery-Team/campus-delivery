using CampusDelivery.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ComplaintProcessViewModel
{
    public int ComplaintId { get; set; }
    public int RecordId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = "SUBMITTED";

    [Required(ErrorMessage = "请选择处理状态")]
    [Display(Name = "处理状态")]
    public string ProcessStatus { get; set; } = "PROCESSING";

    [StringLength(500, ErrorMessage = "处理结果不能超过500字")]
    [Display(Name = "处理结果")]
    public string? ProcessResult { get; set; }

    public static ComplaintProcessViewModel FromModel(Complaint complaint)
    {
        return new ComplaintProcessViewModel
        {
            ComplaintId = complaint.ComplaintId,
            RecordId = complaint.RecordId,
            Reason = complaint.Reason,
            CurrentStatus = complaint.ProcessStatus,
            ProcessStatus = complaint.ProcessStatus == "SUBMITTED" ? "PROCESSING" : complaint.ProcessStatus,
            ProcessResult = complaint.ProcessResult
        };
    }
}
