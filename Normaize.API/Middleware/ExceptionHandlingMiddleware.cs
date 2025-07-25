using Normaize.Core.Interfaces;
using System.Net;
using System.Text.Json;

namespace Normaize.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Get logging service from service provider
            var loggingService = context.RequestServices.GetRequiredService<IStructuredLoggingService>();

            // Generate correlation ID for request tracking
            var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString();

            // Log the exception with full context
            loggingService.LogException(ex, $"Global exception handler - {context.Request.Method} {context.Request.Path} [CorrelationId: {correlationId}]");

            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            Error = new
            {
                Message = GetUserFriendlyMessage(exception),
                Type = exception.GetType().Name,
                Details = exception.Message,
                CorrelationId = correlationId,
                RequestPath = context.Request.Path.ToString(),
                RequestMethod = context.Request.Method,
                Timestamp = DateTime.UtcNow
            }
        };

        context.Response.StatusCode = GetHttpStatusCode(exception);

        var jsonResponse = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Invalid request parameters provided.",
            UnauthorizedAccessException => "You are not authorized to perform this action.",
            InvalidOperationException => "The requested operation cannot be completed.",
            KeyNotFoundException => "The requested resource was not found.",
            NotSupportedException => "This operation is not supported.",
            TimeoutException => "The operation timed out. Please try again.",
            _ => "An unexpected error occurred while processing your request."
        };
    }

    private static int GetHttpStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            NotSupportedException => (int)HttpStatusCode.MethodNotAllowed,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}