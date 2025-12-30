using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Reflection;

namespace Normaize.DataNormalization.Infrastructure.Telemetry;

/// <summary>
/// Configuration for OpenTelemetry distributed tracing and metrics
/// </summary>
public static class OpenTelemetryConfiguration
{
    /// <summary>
    /// Configures OpenTelemetry tracing with instrumentation for ASP.NET Core, HttpClient, and EF Core
    /// </summary>
    public static IServiceCollection AddOpenTelemetryTracing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddSource("Normaize.*")
                .SetResourceBuilder(CreateResourceBuilder(configuration))
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request.method", request.Method);
                        activity.SetTag("http.request.path", request.Path);
                        activity.SetTag("http.request.query_string", request.QueryString.ToString());
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.response.status_code", response.StatusCode);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                })
                .AddConsoleExporter()); // Development only

        return services;
    }

    /// <summary>
    /// Creates a resource builder with service information
    /// </summary>
    private static ResourceBuilder CreateResourceBuilder(IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? configuration["ASPNETCORE_ENVIRONMENT"]
            ?? "Development";

        return ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: "Normaize.API",
                serviceVersion: GetVersion())
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment,
                ["service.name"] = "Normaize.API",
                ["service.version"] = GetVersion()
            });
    }

    /// <summary>
    /// Gets the application version from assembly information
    /// </summary>
    private static string GetVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }
}

