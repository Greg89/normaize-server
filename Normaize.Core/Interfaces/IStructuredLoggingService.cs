using Microsoft.Extensions.Logging;

namespace Normaize.Core.Interfaces;

public interface IStructuredLoggingService
{
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