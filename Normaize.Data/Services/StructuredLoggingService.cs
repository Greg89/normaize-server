using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Normaize.Data.Services;

/// <summary>
/// Implementation of structured logging service for batching log operations.
/// Reduces logging overhead while maintaining detailed information.
/// </summary>
public class StructuredLoggingService : IStructuredLoggingService
{
    private readonly ILogger<StructuredLoggingService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StructuredLoggingService(ILogger<StructuredLoggingService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public IOperationContext CreateContext(string operationName, string correlationId, string? userId = null, Dictionary<string, object>? additionalContext = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(operationName);
        ArgumentException.ThrowIfNullOrEmpty(correlationId);

        return new OperationContext
        {
            OperationName = operationName,
            CorrelationId = correlationId,
            UserId = userId ?? AppConstants.Messages.UNKNOWN,
            Metadata = additionalContext ?? []
        };
    }

    public void LogStep(IOperationContext context, string step, Dictionary<string, object>? additionalData = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(step);

        context.Steps.Add(step);
        
        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                context.SetMetadata(kvp.Key, kvp.Value);
            }
        }
    }

    public void LogSummary(IOperationContext context, bool isSuccess, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var logData = new Dictionary<string, object>
        {
            ["OperationName"] = context.OperationName,
            ["CorrelationId"] = context.CorrelationId,
            ["UserId"] = context.UserId ?? AppConstants.Messages.UNKNOWN,
            ["Steps"] = context.Steps,
            ["Duration"] = context.Stopwatch.Elapsed.TotalMilliseconds,
            ["Success"] = isSuccess
        };

        // Add all metadata
        foreach (var kvp in context.Metadata)
        {
            logData[kvp.Key] = kvp.Value;
        }

        if (isSuccess)
        {
            _logger.LogInformation("Operation completed successfully. {@OperationData}", logData);
        }
        else
        {
            logData["ErrorMessage"] = errorMessage ?? AppConstants.Messages.UNKNOWN;
            _logger.LogError("Operation failed. {@OperationData}", logData);
        }
    }

    public void LogImmediateStep(IOperationContext context, string step, LogLevel level = LogLevel.Information, Dictionary<string, object>? additionalData = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(step);

        var logData = new Dictionary<string, object>
        {
            ["OperationName"] = context.OperationName,
            ["CorrelationId"] = context.CorrelationId,
            ["UserId"] = context.UserId ?? AppConstants.Messages.UNKNOWN,
            ["Step"] = step,
            ["Duration"] = context.Stopwatch.Elapsed.TotalMilliseconds
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                logData[kvp.Key] = kvp.Value;
            }
        }

        _logger.Log(level, "Operation step: {Step}. {@StepData}", step, logData);
    }

    // Backward compatibility methods
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
        ArgumentNullException.ThrowIfNull(exception);

        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var requestContext = GetRequestContext();
        
        var exceptionData = new
        {
            Context = context,
            UserId = userId ?? AppConstants.Auth.AnonymousUser,
            UserEmail = userEmail ?? AppConstants.Messages.UNKNOWN,
            RequestPath = requestContext.Path,
            RequestMethod = requestContext.Method,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            exception.StackTrace,
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
            UserEmail = userEmail ?? AppConstants.Messages.UNKNOWN,
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
        // For now, return a no-op disposable since we're not using Serilog contexts
        return new NoOpDisposable();
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
            return (AppConstants.Messages.UNKNOWN, AppConstants.Messages.UNKNOWN);
        
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

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private class OperationContext : IOperationContext
    {
        public string OperationName { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = [];
        public List<string> Steps { get; set; } = [];
        public Stopwatch Stopwatch { get; set; } = Stopwatch.StartNew();

        public void SetMetadata(string key, object value)
        {
            Metadata[key] = value;
        }

        public T? GetMetadata<T>(string key, T? defaultValue = default)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }
} 