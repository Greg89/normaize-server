using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Normaize.DataNormalization.Infrastructure.Middleware;

/// <summary>
/// Middleware that generates and propagates correlation IDs for request tracing
/// </summary>
public class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdLogPropertyName = "CorrelationId";
    private const string CorrelationIdHttpContextKey = "CorrelationId";

    private readonly RequestDelegate _next = next;
    private readonly ILogger<CorrelationIdMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);

        // Store in HttpContext for access by other middleware/services
        context.Items[CorrelationIdHttpContextKey] = correlationId;

        // Add to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Push to Serilog LogContext so all logs in this request include the correlation ID
        using (LogContext.PushProperty(CorrelationIdLogPropertyName, correlationId))
        {
            // Also add to Activity (for OpenTelemetry tracing - Phase 2)
            Activity.Current?.SetTag(CorrelationIdLogPropertyName, correlationId);

            await _next(context);
        }
    }

    /// <summary>
    /// Gets correlation ID from request header or generates a new one
    /// </summary>
    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Check if correlation ID is already in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader) &&
            !string.IsNullOrWhiteSpace(correlationIdHeader))
        {
            return correlationIdHeader.ToString();
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}

