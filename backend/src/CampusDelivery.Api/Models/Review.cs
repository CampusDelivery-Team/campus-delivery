using System;

namespace CampusDelivery.Api.Models;

public class Review
{
    public int ReviewID { get; set; }
    public int? ReportID { get; set; }
    public int? Rating { get; set; }
    public char Anonymous_flag {  get; set; }

    public string? Comment_text { get; set; }
    public DateTime Reviewed_at { get; set; }
    public int Credit_delta { get; set; }
}

