namespace CampusDelivery.Api.Models;

public static class TaskStatusCodes
{
    public const string WaitConfirm = "WAIT_CONFIRM";
    public const string Finished = "FINISHED";
    public const string Cancelled = "CANCELLED";
    public const string Refunding = "REFUNDING";
}

public static class AccountStatusCodes
{
    public const string Normal = "NORMAL";
    public const string Blocked = "BLOCKED";
    public const string Cancelled = "CANCELLED";
}

public static class PaymentStatusCodes
{
    public const string Unpaid = "UNPAID";
    public const string Paid = "PAID";
    public const string Failed = "FAILED";
    public const string Refunded = "REFUNDED";
}

public static class RefundStatusCodes
{
    public const string Apply = "APPLY";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Done = "DONE";

    public static bool IsActive(string status) => status is Apply or Approved;
}

public static class SettlementStatusCodes
{
    public const string Waiting = "WAITING";
    public const string Done = "DONE";
    public const string Blocked = "BLOCKED";

    public static bool IsKnown(string status) => status is Waiting or Done or Blocked;

    public static bool CanTransition(string currentStatus, string targetStatus) =>
        (currentStatus, targetStatus) switch
        {
            (Waiting, Done) => true,
            (Waiting, Blocked) => true,
            (Blocked, Waiting) => true,
            _ => false
        };
}

public static class ReportStatusCodes
{
    public const string Generated = "GENERATED";
    public const string Exported = "EXPORTED";
}

public static class ComplaintStatusCodes
{
    public const string Submitted = "SUBMITTED";
    public const string Processing = "PROCESSING";
    public const string Done = "DONE";
}
