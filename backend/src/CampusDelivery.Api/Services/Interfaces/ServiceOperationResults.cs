namespace CampusDelivery.Api.Services.Interfaces;

public enum UserRegistrationFailure
{
    None,
    DuplicateUsername,
    DuplicatePhone,
    Unavailable
}

public sealed record UserRegistrationResult(
    bool Success,
    string ErrorMessage,
    UserRegistrationFailure Failure = UserRegistrationFailure.None);

public sealed record AuditOperationResult(bool Success, string Message);

public enum NodeUpdateResult
{
    Success,
    NotFound,
    DuplicateName
}

public enum NodeStatusUpdateResult
{
    Success,
    NotFound,
    NoChange
}

public enum NodeDeleteOperationResult
{
    Success,
    NotFound,
    Referenced
}

public sealed record PaymentOperationResult(bool Success, string ErrorMessage, int PaymentId);

public sealed record RefundOperationResult(bool Success, string ErrorMessage, int RefundId);

public sealed record ReportOperationResult(bool Success, string Message, int? ReportId);

public enum RunnerApplicationResult
{
    Success,
    AlreadyExists,
    AccountUnavailable
}

public enum RunnerReviewResult
{
    Success,
    NotFound,
    AlreadyReviewed,
    AccountUnavailable
}

public enum RunnerWorkStatusResult
{
    Success,
    NotFound,
    Busy,
    Unavailable
}

public enum ServiceNodeRuleCreateResult
{
    Success,
    Duplicate,
    Unavailable
}

public enum ServiceNodeRuleRemoveResult
{
    Success,
    NotFound,
    Referenced
}

public enum ServiceTypeUpdateResult
{
    Success,
    NotFound,
    DuplicateName
}

public enum ServiceTypeDeleteOperationResult
{
    Success,
    NotFound,
    Referenced
}

public sealed record SettlementOperationResult(bool Success, string Message, int? SettlementId);

public sealed class TaskOperationResult(bool success, string errorMessage, int taskId = 0)
{
    public bool Success { get; } = success;

    public string ErrorMessage { get; } = errorMessage;

    public int TaskId { get; } = taskId;
}
