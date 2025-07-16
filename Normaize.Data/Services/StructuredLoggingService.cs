using Serilog.Context;
using System.Security.Claims;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Normaize.Data.Services;

public class StructuredLoggingService : IStructuredLoggingService
{
    private readonly ILogger<StructuredLoggingService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StructuredLoggingService(ILogger<StructuredLoggingService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public void LogUserAction(string action, object? data = null)
    {
        LogUserAction(action, data, LogLevel.Information);
    }

    public void LogUserAction(string action, object? data, LogLevel level)
    {
        ValidateInput(action, nameof(action));
        
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var requestContext = GetRequestContext();
        
        var logData = new
        {
            Action = action,
            UserId = userId ?? AppConstants.Auth.AnonymousUser,
            UserEmail = userEmail ?? "unknown",
            RequestPath = requestContext.Path,
            RequestMethod = requestContext.Method,
            Timestamp = DateTime.UtcNow
        };
        
        _logger.Log(level, "User Action: {Action} by User: {UserId} ({UserEmail}) - Request: {Method} {Path}", 
            action, logData.UserId, logData.UserEmail, requestContext.Method, requestContext.Path);
        
        if (data != null)
        {
            _logger.Log(level, "Action Data: {@ActionData}", data);
        }
    }

    public void LogException(Exception exception, string context = "")
    {
        LogException(exception, context, LogLevel.Error);
    }

    public void LogException(Exception exception, string context, LogLevel level)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var requestContext = GetRequestContext();
        
        var exceptionData = new
        {
            Context = context,
            UserId = userId ?? AppConstants.Auth.AnonymousUser,
            UserEmail = userEmail ?? "unknown",
            RequestPath = requestContext.Path,
            RequestMethod = requestContext.Method,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = exception.StackTrace,
            Timestamp = DateTime.UtcNow
        };

        _logger.Log(level, exception, 
            "Exception in {Context} - User: {UserId} ({UserEmail}) - Request: {Method} {Path} - Type: {ExceptionType} - Message: {ExceptionMessage}", 
            context, exceptionData.UserId, exceptionData.UserEmail, requestContext.Method, requestContext.Path, 
            exceptionData.ExceptionType, exceptionData.ExceptionMessage);
    }

    public void LogRequestStart(string method, string path, string? userId = null)
    {
        LogRequestStart(method, path, userId, LogLevel.Information);
    }

    public void LogRequestStart(string method, string path, string? userId, LogLevel level)
    {
        ValidateInput(method, nameof(method));
        ValidateInput(path, nameof(path));
        
        var actualUserId = userId ?? GetCurrentUserId() ?? AppConstants.Auth.AnonymousUser;
        
        var requestData = new
        {
            Method = method,
            Path = path,
            UserId = actualUserId,
            Timestamp = DateTime.UtcNow
        };
        
        _logger.Log(level, "Request Started: {Method} {Path} by User: {UserId}", 
            method, path, actualUserId);
    }

    public void LogRequestEnd(string method, string path, int statusCode, long durationMs, string? userId = null)
    {
        LogRequestEnd(method, path, statusCode, durationMs, userId, LogLevel.Information);
    }

    public void LogRequestEnd(string method, string path, int statusCode, long durationMs, string? userId, LogLevel level)
    {
        ValidateInput(method, nameof(method));
        ValidateInput(path, nameof(path));
        ValidateStatusCode(statusCode);
        ValidateDuration(durationMs);
        
        var actualUserId = userId ?? GetCurrentUserId() ?? AppConstants.Auth.AnonymousUser;
        
        var requestData = new
        {
            Method = method,
            Path = path,
            StatusCode = statusCode,
            DurationMs = durationMs,
            UserId = actualUserId,
            Timestamp = DateTime.UtcNow
        };
        
        // Determine log level based on status code
        var finalLevel = statusCode >= 400 ? LogLevel.Warning : level;
        
        _logger.Log(finalLevel, "Request Completed: {Method} {Path} - Status: {StatusCode} - Duration: {DurationMs}ms - User: {UserId}", 
            method, path, statusCode, durationMs, actualUserId);
    }

    public void LogPerformance(string operation, long durationMs, object? metadata = null)
    {
        LogPerformance(operation, durationMs, metadata, LogLevel.Information);
    }

    public void LogPerformance(string operation, long durationMs, object? metadata, LogLevel level)
    {
        ValidateInput(operation, nameof(operation));
        ValidateDuration(durationMs);
        
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        
        var performanceData = new
        {
            Operation = operation,
            DurationMs = durationMs,
            UserId = userId ?? AppConstants.Auth.AnonymousUser,
            UserEmail = userEmail ?? "unknown",
            Timestamp = DateTime.UtcNow
        };
        
        _logger.Log(level, "Performance: {Operation} completed in {DurationMs}ms by User: {UserId} ({UserEmail})", 
            operation, durationMs, performanceData.UserId, performanceData.UserEmail);
        
        if (metadata != null)
        {
            _logger.Log(level, "Performance Metadata: {@Metadata}", metadata);
        }
    }

    public IDisposable CreateUserScope(string? userId, string? userEmail)
    {
        return LogContext.PushProperty("UserEmail", userEmail ?? "unknown");
    }

    private string? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;
        var user = httpContext.User;
        if (user == null)
            return null;
        
        // Try to get from claims first
        var userIdFromClaims = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdFromClaims))
            return userIdFromClaims;
        
        // Fallback to sub claim (Auth0)
        var subClaim = user.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
            return subClaim;
        
        // Fallback to name claim
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    private string? GetCurrentUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;
        var user = httpContext.User;
        if (user == null)
            return null;
        
        // Try to get from claims first
        var emailFromClaims = user.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(emailFromClaims))
            return emailFromClaims;
        
        // Fallback to Auth0 email claim
        return user.FindFirst("email")?.Value;
    }

    private (string Method, string Path) GetRequestContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return ("unknown", "unknown");
        
        return (httpContext.Request.Method, httpContext.Request.Path.ToString());
    }

    private static void ValidateInput(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
    }

    private static void ValidateStatusCode(int statusCode)
    {
        if (statusCode < 100 || statusCode > 599)
            throw new ArgumentException("Status code must be between 100 and 599", nameof(statusCode));
    }

    private static void ValidateDuration(long durationMs)
    {
        if (durationMs < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(durationMs));
    }
} 