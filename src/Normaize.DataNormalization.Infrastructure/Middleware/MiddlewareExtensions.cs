using Microsoft.AspNetCore.Builder;

namespace Normaize.DataNormalization.Infrastructure.Middleware;

/// <summary>
/// Extension methods for registering middleware
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the pipeline
    /// This should be registered early in the pipeline to ensure correlation IDs are available for all logs
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}

