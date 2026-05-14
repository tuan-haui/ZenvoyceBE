using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Zenvoyce.Application.Common.Models;

namespace Zenvoyce.API.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ValidationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.BadRequest,
            ArgumentException or ArgumentNullException => HttpStatusCode.BadRequest,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        ApiResponse<object?> envelope = exception switch
        {
            ValidationException vex => ApiResponses.Fail(
                "Dữ liệu không hợp lệ.",
                vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            KeyNotFoundException => ApiResponses.Fail(exception.Message),
            InvalidOperationException => ApiResponses.Fail(exception.Message),
            UnauthorizedAccessException => ApiResponses.Fail("Không có quyền truy cập."),
            ArgumentException or ArgumentNullException => ApiResponses.Fail(exception.Message),
            TimeoutException => ApiResponses.Fail("Yêu cầu hết thời gian chờ. Vui lòng thử lại."),
            _ => ApiResponses.Fail("Đã xảy ra lỗi trong quá trình xử lý. Vui lòng thử lại sau.")
        };

        var response = JsonSerializer.Serialize(envelope, JsonOptions);
        return context.Response.WriteAsync(response);
    }
}
