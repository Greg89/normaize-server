using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Normaize.API.Services;

public interface IStructuredLoggingService
{
    void LogUserAction(string action, object? data = null);
    void LogException(Exception exception, string context = "");
    void LogRequestStart(string method, string path, string? userId = null);
    void LogRequestEnd(string method, string path, int statusCode, long durationMs);
    IDisposable CreateUserScope(string? userId, string? userEmail);
}

public class StructuredLoggingService : IStructuredLoggingService
{
    private readonly Microsoft.Extensions.Logging.ILogger<StructuredLoggingService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StructuredLoggingService(Microsoft.Extensions.Logging.ILogger<StructuredLoggingService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void LogUserAction(string action, object? data = null)
    {
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        
        _logger.LogInformation("User Action: {Action} by User: {UserId} ({UserEmail})", 
            action, userId ?? "anonymous", userEmail ?? "unknown");
        
        if (data != null)
        {
            _logger.LogInformation("Action Data: {@ActionData}", data);
        }
    }

    public void LogException(Exception exception, string context = "")
    {
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var requestPath = _httpContextAccessor.HttpContext?.Request.Path;
        var requestMethod = _httpContextAccessor.HttpContext?.Request.Method;

        _logger.LogError(exception, 
            "Exception in {Context} - User: {UserId} ({UserEmail}) - Request: {Method} {Path}", 
            context, userId ?? "anonymous", userEmail ?? "unknown", requestMethod, requestPath);
    }

    public void LogRequestStart(string method, string path, string? userId = null)
    {
        _logger.LogInformation("Request Started: {Method} {Path} by User: {UserId}", 
            method, path, userId ?? "anonymous");
    }

    public void LogRequestEnd(string method, string path, int statusCode, long durationMs)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Request Completed: {Method} {Path} - Status: {StatusCode} - Duration: {DurationMs}ms - User: {UserId}", 
            method, path, statusCode, durationMs, userId ?? "anonymous");
    }

    public IDisposable CreateUserScope(string? userId, string? userEmail)
    {
        var context = LogContext.PushProperty("UserId", userId ?? "anonymous");
        return LogContext.PushProperty("UserEmail", userEmail ?? "unknown");
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.Items["UserId"]?.ToString();
    }

    private string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.Items["UserEmail"]?.ToString();
    }
} 