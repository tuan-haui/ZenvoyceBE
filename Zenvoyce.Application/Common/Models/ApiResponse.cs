namespace Zenvoyce.Application.Common.Models;

/// <summary>
/// Envelope JSON thống nhất cho mọi API: success, data, message, errors.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse<T> Ok(T? data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message,
        Errors = null
    };

    public static ApiResponse<T> Fail(string message, IDictionary<string, string[]>? errors = null) => new()
    {
        Success = false,
        Data = default,
        Message = message,
        Errors = errors
    };
}

/// <summary>
/// Helper tạo envelope không cần suy luận kiểu T khi bọc object động (filter).
/// </summary>
public static class ApiResponses
{
    public static ApiResponse<object?> Ok(object? data, string? message = null) =>
        ApiResponse<object?>.Ok(data, message);

    public static ApiResponse<object?> Fail(string message, IDictionary<string, string[]>? errors = null) =>
        ApiResponse<object?>.Fail(message, errors);
}
