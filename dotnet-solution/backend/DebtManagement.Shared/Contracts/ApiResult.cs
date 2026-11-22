namespace DebtManagement.Shared.Contracts;

public record ApiResult(bool Success, string? Message = null, IReadOnlyList<string>? Errors = null)
{
    public static ApiResult Ok(string? message = null) => new(true, message);
    public static ApiResult Fail(string message, IReadOnlyList<string>? errors = null) => new(false, message, errors);
}

public record ApiResult<T>(bool Success, T? Data = default, string? Message = null, IReadOnlyList<string>? Errors = null)
{
    public static ApiResult<T> Ok(T data, string? message = null) => new(true, data, message);
    public static ApiResult<T> Fail(string message, IReadOnlyList<string>? errors = null) => new(false, default, message, errors);
}