namespace CampusDelivery.Api.Repositories.Interfaces;

public enum UserInsertWriteResult
{
    Success,
    DuplicateUsername,
    DuplicatePhone,
    Failed
}

public enum UserPhoneUpdateWriteResult
{
    Success,
    NotFound,
    DuplicatePhone
}

public enum NodeDeleteResult
{
    Success,
    NotFound,
    Referenced
}

public enum ServiceTypeDeleteResult
{
    Success,
    NotFound,
    Referenced
}

public enum ServiceNodeRuleRemoveResult
{
    Success,
    NotFound,
    Referenced
}

public enum RunnerReviewWriteResult
{
    Success,
    NotFound,
    AlreadyReviewed,
    AccountUnavailable
}

public enum RunnerWorkStatusWriteResult
{
    Success,
    NotFound,
    Busy,
    Unavailable
}

public sealed class TaskCreateWriteResult(TaskCreateResult result, int taskId = 0)
{
    public TaskCreateResult Result { get; } = result;

    public int TaskId { get; } = taskId;
}

public enum TaskCreateResult
{
    Success,
    AddressNotFound,
    ServiceTypeUnavailable,
    NodeUnavailable,
    RuleNotMatched
}

public enum TaskCancelResult
{
    Success,
    NotFound,
    InvalidState
}
