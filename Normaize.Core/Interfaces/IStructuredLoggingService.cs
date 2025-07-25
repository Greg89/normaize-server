using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for structured logging operations with batching capabilities.
/// Reduces logging overhead while maintaining detailed information.
/// </summary>
public interface IStructuredLoggingService
{
    /// <summary>
    /// Creates a new operation context for structured logging.
    /// </summary>
    /// <param name="operationName">Name of the operation being logged</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="additionalContext">Additional context data</param>
    /// <returns>Operation context for collecting log data</returns>
    IOperationContext CreateContext(string operationName, string correlationId, string? userId = null, Dictionary<string, object>? additionalContext = null);

    /// <summary>
    /// Logs a step in the operation without immediately writing to logs.
    /// </summary>
    /// <param name="context">Operation context</param>
    /// <param name="step">Description of the step</param>
    /// <param name="additionalData">Additional data to store with this step</param>
    void LogStep(IOperationContext context, string step, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Logs the final summary of the operation with all collected information.
    /// </summary>
    /// <param name="context">Operation context</param>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="errorMessage">Error message if operation failed</param>
    void LogSummary(IOperationContext context, bool isSuccess, string? errorMessage = null);

    /// <summary>
    /// Logs an operation step and immediately writes to logs (for critical steps).
    /// </summary>
    /// <param name="context">Operation context</param>
    /// <param name="step">Description of the step</param>
    /// <param name="level">Log level</param>
    /// <param name="additionalData">Additional data</param>
    void LogImmediateStep(IOperationContext context, string step, LogLevel level = LogLevel.Information, Dictionary<string, object>? additionalData = null);

    // Backward compatibility methods for existing code
    void LogUserAction(string action, object? data = null);
    void LogUserAction(string action, object? data, LogLevel level);
    void LogException(Exception exception, string context = "");
    void LogException(Exception exception, string context, LogLevel level);
    void LogRequestStart(string method, string path, string? userId = null);
    void LogRequestStart(string method, string path, string? userId, LogLevel level);
    void LogRequestEnd(string method, string path, int statusCode, long durationMs, string? userId = null);
    void LogRequestEnd(string method, string path, int statusCode, long durationMs, string? userId, LogLevel level);
    void LogPerformance(string operation, long durationMs, object? metadata = null);
    void LogPerformance(string operation, long durationMs, object? metadata, LogLevel level);
    IDisposable CreateUserScope(string? userId, string? userEmail);
}

/// <summary>
/// Context for collecting structured log data during an operation.
/// </summary>
public interface IOperationContext
{
    /// <summary>
    /// Name of the operation being logged.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Correlation ID for tracing the operation.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// User ID performing the operation.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Additional context data collected during the operation.
    /// </summary>
    Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Steps performed during the operation.
    /// </summary>
    List<string> Steps { get; }

    /// <summary>
    /// Stopwatch for measuring operation duration.
    /// </summary>
    Stopwatch Stopwatch { get; }

    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    void SetMetadata(string key, object value);

    /// <summary>
    /// Gets a metadata value.
    /// </summary>
    /// <typeparam name="T">Expected type of the value</typeparam>
    /// <param name="key">Metadata key</param>
    /// <param name="defaultValue">Default value if key doesn't exist</param>
    /// <returns>The metadata value or default</returns>
    T? GetMetadata<T>(string key, T? defaultValue = default);
}