using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Normaize.DataNormalization.API.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, errorCode, message) = MapException(ex);

            logger.LogError(ex, "Unhandled exception in request {Method} {Path}. Returning {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                statusCode);

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var correlationId = GetCorrelationId(context);

                var response = new Normaize.DataNormalization.API.Controllers.ApiResponse<object?>
                {
                    Success = false,
                    Data = null,
                    Message = message,
                    ErrorCode = errorCode,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    DurationMs = 0
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
                return;
            }

            throw;
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Response.Headers.TryGetValue(CorrelationIdHeaderName, out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return context.TraceIdentifier;
    }

    private static (int StatusCode, string ErrorCode, string Message) MapException(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "You are not authorized to perform this action"),
            ArgumentException => (StatusCodes.Status400BadRequest, "INVALID_ARGUMENT", ex.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "INVALID_OPERATION", ex.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", "The requested resource was not found"),
            NotSupportedException => (StatusCodes.Status405MethodNotAllowed, "NOT_SUPPORTED", "This operation is not supported"),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "TIMEOUT", "The operation timed out. Please try again"),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred while processing your request")
        };
    }
}
