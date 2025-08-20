using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.Core.Extensions;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Normaize.Data.Services;

/// <summary>
/// Implementation of structured logging service for batching log operations.
/// </summary>
/// <remarks>
/// This service provides comprehensive structured logging capabilities including:
/// - Operation context management with correlation IDs
/// - Step-by-step operation logging with batching
/// - User action tracking with request context
/// - Performance monitoring and exception logging
/// - Backward compatibility with existing logging patterns
/// 
/// The service reduces logging overhead while maintaining detailed information
/// through batching and structured data collection. It integrates with the
/// HTTP context to provide request-specific information and user context.
/// 
/// Key features:
/// - Structured operation logging with metadata collection
/// - User action tracking with authentication context
/// - Performance monitoring with duration tracking
/// - Exception logging with full context information
/// - Request lifecycle logging (start/end)
/// - Flexible log level control for different scenarios
/// </remarks>
public class StructuredLoggingService : IStructuredLoggingService
{
    private readonly ILogger<StructuredLoggingService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the StructuredLoggingService
    /// </summary>
    /// <param name="logger">Logger instance for writing log entries</param>
    /// <param name="httpContextAccessor">HTTP context accessor for request information</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or httpContextAccessor is null</exception>
    public StructuredLoggingService(ILogger<StructuredLoggingService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    #region Structured Logging Operations

    /// <summary>
    /// Creates a new operation context for structured logging
    /// </summary>
    /// <param name="operationName">Name of the operation being logged</param>
    /// <param name="correlationId">Correlation ID for tracing the operation</param>
    /// <param name="userId">User ID performing the operation (optional)</param>
    /// <param name="additionalContext">Additional context data to include (optional)</param>
    /// <returns>Operation context for collecting log data</returns>
    /// <exception cref="ArgumentException">Thrown when operationName or correlationId is null or empty</exception>
    /// <remarks>
    /// Creates a new operation context that tracks:
    /// - Operation name and correlation ID for tracing
    /// - User context and additional metadata
    /// - Step-by-step progress with timing
    /// - Final summary with success/failure status
    /// 
    /// The context should be used with LogStep, LogImmediateStep, and LogSummary
    /// methods to build a complete operation log entry.
    /// </remarks>
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

    /// <summary>
    /// Logs a step in the operation without immediately writing to logs
    /// </summary>
    /// <param name="context">Operation context to add the step to</param>
    /// <param name="step">Description of the step being performed</param>
    /// <param name="additionalData">Additional data to store with this step (optional)</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
    /// <exception cref="ArgumentException">Thrown when step is null or empty</exception>
    /// <remarks>
    /// This method collects step information without writing to logs immediately.
    /// The step and any additional data are stored in the context and will be
    /// included when LogSummary is called. This approach reduces logging overhead
    /// while maintaining detailed operation tracking.
    /// </remarks>
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

    /// <summary>
    /// Logs the final summary of the operation with all collected information
    /// </summary>
    /// <param name="context">Operation context containing collected data</param>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="errorMessage">Error message if operation failed (optional)</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
    /// <remarks>
    /// This method writes the complete operation summary to logs, including:
    /// - All collected steps and metadata
    /// - Operation duration from the stopwatch
    /// - Success/failure status and error details
    /// - User context and correlation information
    /// 
    /// This is typically called at the end of an operation to provide a complete
    /// audit trail of what occurred during execution.
    /// </remarks>
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

    /// <summary>
    /// Logs an operation step and immediately writes to logs (for critical steps)
    /// </summary>
    /// <param name="context">Operation context for the operation</param>
    /// <param name="step">Description of the step being performed</param>
    /// <param name="level">Log level for the step (defaults to Information)</param>
    /// <param name="additionalData">Additional data to include with the step (optional)</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
    /// <exception cref="ArgumentException">Thrown when step is null or empty</exception>
    /// <remarks>
    /// This method immediately writes a step to logs, which is useful for:
    /// - Critical steps that need immediate visibility
    /// - Long-running operations where real-time progress is needed
    /// - Debugging scenarios requiring immediate feedback
    /// 
    /// The step is also added to the context for inclusion in the final summary.
    /// </remarks>
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

    #endregion

    #region Backward Compatibility Methods

    /// <summary>
    /// Logs a user action with default Information log level
    /// </summary>
    /// <param name="action">Description of the action being performed</param>
    /// <param name="data">Additional data related to the action (optional)</param>
    /// <exception cref="ArgumentException">Thrown when action is null or empty</exception>
    /// <remarks>
    /// This method provides backward compatibility for existing code that logs
    /// user actions. It automatically includes user context and request information
    /// from the current HTTP context.
    /// </remarks>
    public void LogUserAction(string action, object? data = null)
    {
        LogUserAction(action, data, LogLevel.Information);
    }

    /// <summary>
    /// Logs a user action with specified log level
    /// </summary>
    /// <param name="action">Description of the action being performed</param>
    /// <param name="data">Additional data related to the action (optional)</param>
    /// <param name="level">Log level for the action</param>
    /// <exception cref="ArgumentException">Thrown when action is null or empty</exception>
    /// <remarks>
    /// Logs user actions with full context including:
    /// - User identification and email
    /// - Request path and method
    /// - Timestamp and additional data
    /// - Specified log level for appropriate visibility
    /// </remarks>
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

    /// <summary>
    /// Logs an exception with default Error log level
    /// </summary>
    /// <param name="exception">Exception to log</param>
    /// <param name="context">Context where the exception occurred (optional)</param>
    /// <exception cref="ArgumentNullException">Thrown when exception is null</exception>
    /// <remarks>
    /// Provides backward compatibility for exception logging with automatic
    /// context extraction from the current HTTP request and user session.
    /// </remarks>
    public void LogException(Exception exception, string context = "")
    {
        LogException(exception, context, LogLevel.Error);
    }

    /// <summary>
    /// Logs an exception with specified log level
    /// </summary>
    /// <param name="exception">Exception to log</param>
    /// <param name="context">Context where the exception occurred (optional)</param>
    /// <param name="level">Log level for the exception</param>
    /// <exception cref="ArgumentNullException">Thrown when exception is null</exception>
    /// <remarks>
    /// Logs exceptions with comprehensive context including:
    /// - Exception type, message, and stack trace
    /// - User context and request information
    /// - Timestamp and context description
    /// - Specified log level for appropriate handling
    /// </remarks>
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

    /// <summary>
    /// Logs the start of an HTTP request with default Information log level
    /// </summary>
    /// <param name="method">HTTP method of the request</param>
    /// <param name="path">Request path</param>
    /// <param name="userId">User ID making the request (optional)</param>
    /// <exception cref="ArgumentException">Thrown when method or path is null or empty</exception>
    /// <remarks>
    /// Logs the beginning of HTTP requests for tracking request lifecycle
    /// and user activity patterns.
    /// </remarks>
    public void LogRequestStart(string method, string path, string? userId = null)
    {
        LogRequestStart(method, path, userId, LogLevel.Information);
    }

    /// <summary>
    /// Logs the start of an HTTP request with specified log level
    /// </summary>
    /// <param name="method">HTTP method of the request</param>
    /// <param name="path">Request path</param>
    /// <param name="userId">User ID making the request (optional)</param>
    /// <param name="level">Log level for the request start</param>
    /// <exception cref="ArgumentException">Thrown when method or path is null or empty</exception>
    /// <remarks>
    /// Tracks request initiation with user context and specified visibility level.
    /// Useful for monitoring request patterns and user activity.
    /// </remarks>
    public void LogRequestStart(string method, string path, string? userId, LogLevel level)
    {
        ValidateInput(method, nameof(method));
        ValidateInput(path, nameof(path));

        var actualUserId = userId ?? GetCurrentUserId() ?? AppConstants.Auth.AnonymousUser;

        _logger.Log(level, "Request Started: {Method} {Path} by User: {UserId}",
            method, path, actualUserId);
    }

    /// <summary>
    /// Logs the completion of an HTTP request with default Information log level
    /// </summary>
    /// <param name="method">HTTP method of the request</param>
    /// <param name="path">Request path</param>
    /// <param name="statusCode">HTTP status code returned</param>
    /// <param name="durationMs">Request duration in milliseconds</param>
    /// <param name="userId">User ID that made the request (optional)</param>
    /// <exception cref="ArgumentException">Thrown when method, path, statusCode, or durationMs is invalid</exception>
    /// <remarks>
    /// Logs request completion with performance metrics and status information.
    /// Automatically adjusts log level based on status code (Warning for 4xx/5xx).
    /// </remarks>
    public void LogRequestEnd(string method, string path, int statusCode, long durationMs, string? userId = null)
    {
        LogRequestEnd(method, path, statusCode, durationMs, userId, LogLevel.Information);
    }

    /// <summary>
    /// Logs the completion of an HTTP request with specified log level
    /// </summary>
    /// <param name="method">HTTP method of the request</param>
    /// <param name="path">Request path</param>
    /// <param name="statusCode">HTTP status code returned</param>
    /// <param name="durationMs">Request duration in milliseconds</param>
    /// <param name="userId">User ID that made the request (optional)</param>
    /// <param name="level">Base log level for the request end</param>
    /// <exception cref="ArgumentException">Thrown when method, path, statusCode, or durationMs is invalid</exception>
    /// <remarks>
    /// Provides comprehensive request completion logging with:
    /// - Performance metrics (duration)
    /// - Status code and success/failure indication
    /// - User context and request details
    /// - Automatic log level adjustment for error status codes
    /// </remarks>
    public void LogRequestEnd(string method, string path, int statusCode, long durationMs, string? userId, LogLevel level)
    {
        ValidateInput(method, nameof(method));
        ValidateInput(path, nameof(path));
        ValidateStatusCode(statusCode);
        ValidateDuration(durationMs);

        var actualUserId = userId ?? GetCurrentUserId() ?? AppConstants.Auth.AnonymousUser;

        // Determine log level based on status code
        var finalLevel = statusCode >= 400 ? LogLevel.Warning : level;

        _logger.Log(finalLevel, "Request Completed: {Method} {Path} - Status: {StatusCode} - Duration: {DurationMs}ms - User: {UserId}",
            method, path, statusCode, durationMs, actualUserId);
    }

    /// <summary>
    /// Logs performance metrics with default Information log level
    /// </summary>
    /// <param name="operation">Name of the operation being measured</param>
    /// <param name="durationMs">Duration of the operation in milliseconds</param>
    /// <param name="metadata">Additional metadata about the operation (optional)</param>
    /// <exception cref="ArgumentException">Thrown when operation is null or empty, or durationMs is negative</exception>
    /// <remarks>
    /// Tracks operation performance for monitoring and optimization purposes.
    /// Includes user context and timing information for comprehensive analysis.
    /// </remarks>
    public void LogPerformance(string operation, long durationMs, object? metadata = null)
    {
        LogPerformance(operation, durationMs, metadata, LogLevel.Information);
    }

    /// <summary>
    /// Logs performance metrics with specified log level
    /// </summary>
    /// <param name="operation">Name of the operation being measured</param>
    /// <param name="durationMs">Duration of the operation in milliseconds</param>
    /// <param name="metadata">Additional metadata about the operation (optional)</param>
    /// <param name="level">Log level for the performance entry</param>
    /// <exception cref="ArgumentException">Thrown when operation is null or empty, or durationMs is negative</exception>
    /// <remarks>
    /// Provides detailed performance logging with:
    /// - Operation identification and duration
    /// - User context and timing information
    /// - Optional metadata for additional context
    /// - Configurable log level for different monitoring needs
    /// </remarks>
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

    /// <summary>
    /// Creates a user scope for logging operations
    /// </summary>
    /// <param name="userId">User ID for the scope (optional)</param>
    /// <param name="userEmail">User email for the scope (optional)</param>
    /// <returns>A disposable scope object</returns>
    /// <remarks>
    /// Currently returns a no-op disposable since we're not using Serilog contexts.
    /// This method is provided for backward compatibility and future extensibility.
    /// </remarks>
    public IDisposable CreateUserScope(string? userId, string? userEmail)
    {
        // For now, return a no-op disposable since we're not using Serilog contexts
        return new NoOpDisposable();
    }

    #endregion

    #region Additional Logging Methods

    /// <summary>
    /// Logs an error message with default Error log level
    /// </summary>
    /// <param name="message">Error message to log</param>
    /// <param name="data">Additional error data (optional)</param>
    /// <exception cref="ArgumentException">Thrown when message is null or empty</exception>
    /// <remarks>
    /// Provides backward compatibility for error logging with automatic
    /// context extraction from the current HTTP request and user session.
    /// </remarks>
    public void LogError(string message, object? data = null)
    {
        LogError(message, data, LogLevel.Error);
    }

    /// <summary>
    /// Logs an error message with specified log level
    /// </summary>
    /// <param name="message">Error message to log</param>
    /// <param name="data">Additional error data (optional)</param>
    /// <param name="level">Log level for the error</param>
    /// <exception cref="ArgumentException">Thrown when message is null or empty</exception>
    /// <remarks>
    /// Logs error messages with comprehensive context including:
    /// - Error message and additional data
    /// - User context and request information
    /// - Timestamp for error tracking
    /// - Specified log level for appropriate handling
    /// </remarks>
    public void LogError(string message, object? data, LogLevel level)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);

        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var requestContext = GetRequestContext();

        var errorData = new
        {
            Message = message,
            UserId = userId ?? AppConstants.Auth.AnonymousUser,
            UserEmail = userEmail ?? AppConstants.Messages.UNKNOWN,
            RequestPath = requestContext.Path,
            RequestMethod = requestContext.Method,
            Timestamp = DateTime.UtcNow
        };

        _logger.Log(level, "Error: {Message} - User: {UserId} ({UserEmail}) - Request: {Method} {Path}",
            message, errorData.UserId, errorData.UserEmail, requestContext.Method, requestContext.Path);

        if (data != null)
        {
            _logger.Log(level, "Error Data: {@ErrorData}", data);
        }
    }

    /// <summary>
    /// Logs a warning message with default Warning log level
    /// </summary>
    /// <param name="message">Warning message to log</param>
    /// <param name="data">Additional warning data (optional)</param>
    /// <exception cref="ArgumentException">Thrown when message is null or empty</exception>
    /// <remarks>
    /// Provides backward compatibility for warning logging with automatic
    /// context extraction from the current HTTP request and user session.
    /// </remarks>
    public void LogWarning(string message, object? data = null)
    {
        LogWarning(message, data, LogLevel.Warning);
    }

    /// <summary>
    /// Logs a warning message with specified log level
    /// </summary>
    /// <param name="message">Warning message to log</param>
    /// <param name="data">Additional warning data (optional)</param>
    /// <param name="level">Log level for the warning</param>
    /// <exception cref="ArgumentException">Thrown when message is null or empty</exception>
    /// <remarks>
    /// Logs warning messages with comprehensive context including:
    /// - Warning message and additional data
    /// - User context and request information
    /// - Timestamp for warning tracking
    /// - Specified log level for appropriate visibility
    /// </remarks>
    public void LogWarning(string message, object? data, LogLevel level)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);

        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var requestContext = GetRequestContext();

        var warningData = new
        {
            Message = message,
            UserId = userId ?? AppConstants.Auth.AnonymousUser,
            UserEmail = userEmail ?? AppConstants.Messages.UNKNOWN,
            RequestPath = requestContext.Path,
            RequestMethod = requestContext.Method,
            Timestamp = DateTime.UtcNow
        };

        _logger.Log(level, "Warning: {Message} - User: {UserId} ({UserEmail}) - Request: {Method} {Path}",
            message, warningData.UserId, warningData.UserEmail, requestContext.Method, requestContext.Path);

        if (data != null)
        {
            _logger.Log(level, "Warning Data: {@WarningData}", data);
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets the current user ID from the HTTP context
    /// </summary>
    /// <returns>User ID if available, null otherwise</returns>
    /// <remarks>
    /// Extracts user ID from the current HTTP context using the ClaimsPrincipalExtensions.
    /// Falls back to anonymous user if no user context is available.
    /// </remarks>
    private string? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
            return null;

        return httpContext.User.GetUserIdWithFallback() ?? AppConstants.Auth.AnonymousUser;
    }

    /// <summary>
    /// Gets the current user email from the HTTP context
    /// </summary>
    /// <returns>User email if available, null otherwise</returns>
    /// <remarks>
    /// Extracts user email from the current HTTP context using the ClaimsPrincipalExtensions.
    /// Returns null if no user context or email claim is available.
    /// </remarks>
    private string? GetCurrentUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
            return null;

        return httpContext.User.GetUserEmail();
    }

    /// <summary>
    /// Gets the current request context from the HTTP context
    /// </summary>
    /// <returns>Tuple containing HTTP method and path</returns>
    /// <remarks>
    /// Extracts HTTP method and path from the current request context.
    /// Returns unknown values if no HTTP context is available.
    /// </remarks>
    private (string Method, string Path) GetRequestContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return (AppConstants.Messages.UNKNOWN, AppConstants.Messages.UNKNOWN);

        return (httpContext.Request.Method, httpContext.Request.Path.ToString());
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates that a string input is not null, empty, or whitespace
    /// </summary>
    /// <param name="value">String value to validate</param>
    /// <param name="parameterName">Name of the parameter for error messages</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace</exception>
    /// <remarks>
    /// Provides consistent validation for string parameters across all logging methods.
    /// Ensures that logging calls have meaningful content for audit and debugging purposes.
    /// </remarks>
    private static void ValidateInput(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
    }

    /// <summary>
    /// Validates that an HTTP status code is within valid range
    /// </summary>
    /// <param name="statusCode">HTTP status code to validate</param>
    /// <exception cref="ArgumentException">Thrown when statusCode is outside valid range (100-599)</exception>
    /// <remarks>
    /// Ensures that HTTP status codes are within the valid range defined by HTTP standards.
    /// Prevents logging of invalid status codes that could cause confusion.
    /// </remarks>
    private static void ValidateStatusCode(int statusCode)
    {
        if (statusCode < 100 || statusCode > 599)
            throw new ArgumentException("Status code must be between 100 and 599", nameof(statusCode));
    }

    /// <summary>
    /// Validates that a duration value is non-negative
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds to validate</param>
    /// <exception cref="ArgumentException">Thrown when durationMs is negative</exception>
    /// <remarks>
    /// Ensures that duration values are valid for performance monitoring.
    /// Negative durations would indicate timing errors and should be caught early.
    /// </remarks>
    private static void ValidateDuration(long durationMs)
    {
        if (durationMs < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(durationMs));
    }

    #endregion

    #region Private Classes

    /// <summary>
    /// No-operation disposable for user scope compatibility
    /// </summary>
    /// <remarks>
    /// Provides a no-operation implementation of IDisposable for the CreateUserScope method.
    /// This allows for future extensibility without breaking existing code.
    /// </remarks>
    private class NoOpDisposable : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method for inheritance
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources (none in this case)
                }

                // Dispose unmanaged resources (none in this case)

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer for the NoOpDisposable class
        /// </summary>
        ~NoOpDisposable()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Implementation of operation context for structured logging
    /// </summary>
    /// <remarks>
    /// Provides a concrete implementation of IOperationContext that tracks:
    /// - Operation metadata and correlation information
    /// - Step-by-step progress with timing
    /// - User context and additional data collection
    /// 
    /// This context is used throughout the operation lifecycle to collect
    /// comprehensive information for final logging and audit purposes.
    /// </remarks>
    private sealed class OperationContext : IOperationContext
    {
        /// <summary>
        /// Name of the operation being logged
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Correlation ID for tracing the operation
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// User ID performing the operation
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Additional context data collected during the operation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = [];

        /// <summary>
        /// Steps performed during the operation
        /// </summary>
        public List<string> Steps { get; set; } = [];

        /// <summary>
        /// Stopwatch for measuring operation duration
        /// </summary>
        public Stopwatch Stopwatch { get; set; } = Stopwatch.StartNew();

        /// <summary>
        /// Sets a metadata value for the operation
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <remarks>
        /// Stores additional context information that will be included
        /// in the final operation summary when LogSummary is called.
        /// </remarks>
        public void SetMetadata(string key, object value)
        {
            Metadata[key] = value;
        }

        /// <summary>
        /// Gets a metadata value with type safety
        /// </summary>
        /// <typeparam name="T">Expected type of the value</typeparam>
        /// <param name="key">Metadata key</param>
        /// <param name="defaultValue">Default value if key doesn't exist or type doesn't match</param>
        /// <returns>The metadata value or default</returns>
        /// <remarks>
        /// Provides type-safe access to metadata values with fallback to
        /// default values when keys don't exist or types don't match.
        /// </remarks>
        public T? GetMetadata<T>(string key, T? defaultValue = default)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }

    #endregion
}