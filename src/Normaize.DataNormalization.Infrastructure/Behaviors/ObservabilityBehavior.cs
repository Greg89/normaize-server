using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Normaize.DataNormalization.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that adds observability (logging, timing, tracing) to all MediatR requests
/// This provides centralized telemetry without requiring changes to individual handlers
/// </summary>
public class ObservabilityBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ObservabilityBehavior<TRequest, TResponse>> _logger;

    public ObservabilityBehavior(ILogger<ObservabilityBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString();

        // Push request context to LogContext so all logs in this request include these properties
        using (LogContext.PushProperty("RequestName", requestName))
        using (LogContext.PushProperty("RequestId", requestId))
        {
            // Add to Activity tags for OpenTelemetry tracing (Phase 2)
            Activity.Current?.SetTag("request.name", requestName);
            Activity.Current?.SetTag("request.id", requestId);
            Activity.Current?.SetTag("request.type", typeof(TRequest).FullName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "Handling {RequestName} ({RequestId})",
                    requestName, requestId);

                var response = await next();

                stopwatch.Stop();

                _logger.LogInformation(
                    "Completed {RequestName} ({RequestId}) in {ElapsedMs}ms",
                    requestName, requestId, stopwatch.ElapsedMilliseconds);

                // Add timing to Activity
                Activity.Current?.SetTag("request.duration_ms", stopwatch.ElapsedMilliseconds);
                Activity.Current?.SetTag("request.status", "success");

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "Failed {RequestName} ({RequestId}) after {ElapsedMs}ms: {ErrorMessage}",
                    requestName, requestId, stopwatch.ElapsedMilliseconds, ex.Message);

                // Add error details to Activity
                Activity.Current?.SetTag("request.duration_ms", stopwatch.ElapsedMilliseconds);
                Activity.Current?.SetTag("request.status", "error");
                Activity.Current?.SetTag("error.type", ex.GetType().Name);
                Activity.Current?.SetTag("error.message", ex.Message);
                Activity.Current?.SetTag("error.stack_trace", ex.StackTrace);

                throw;
            }
        }
    }
}

