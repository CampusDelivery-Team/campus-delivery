using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class ComplaintListItemViewModel
{
    public int ComplaintId { get; set; }
    public int RecordId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ProcessStatus { get; set; } = "SUBMITTED";
    public string? ProcessResult { get; set; }

    public string ProcessStatusDisplayName => ProcessStatus switch
    {
        "SUBMITTED" => "待处理",
        "PROCESSING" => "处理中",
        "DONE" => "已处理",
        _ => ProcessStatus
    };

    public static ComplaintListItemViewModel FromModel(Complaint complaint)
    {
        return new ComplaintListItemViewModel
        {
            ComplaintId = complaint.ComplaintId,
            RecordId = complaint.RecordId,
            Reason = complaint.Reason,
            ProcessStatus = complaint.ProcessStatus,
            ProcessResult = complaint.ProcessResult
        };
    }
}
