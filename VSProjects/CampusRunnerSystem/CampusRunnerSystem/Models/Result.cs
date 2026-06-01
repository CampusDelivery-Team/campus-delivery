namespace CampusRunnerSystem.Models;

public class Result
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static Result Ok(string message = "操作成功")
    {
        return new Result { Success = true, Message = message };
    }

    public static Result Fail(string message)
    {
        return new Result { Success = false, Message = message };
    }
}

public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Ok(T data, string message = "操作成功")
    {
        return new Result<T> { Success = true, Message = message, Data = data };
    }

    public new static Result<T> Fail(string message)
    {
        return new Result<T> { Success = false, Message = message };
    }
}
