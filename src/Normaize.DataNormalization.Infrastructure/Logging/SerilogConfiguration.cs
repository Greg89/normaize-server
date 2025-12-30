using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace Normaize.DataNormalization.Infrastructure.Logging;

/// <summary>
/// Configuration for Serilog structured logging with enrichment and multiple sinks
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configures Serilog on the host builder (must be called before UseSerilog)
    /// </summary>
    public static IHostBuilder ConfigureSerilog(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        return hostBuilder.UseSerilog((context, services, config) =>
        {
            ConfigureLogger(config, configuration);
        });
    }

    /// <summary>
    /// Configures Serilog with structured logging, enrichment, and multiple sinks
    /// This method can be used to configure Serilog after services are built
    /// </summary>
    public static IServiceCollection AddStructuredLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // If Log.Logger hasn't been configured yet, configure it now
        if (Log.Logger.GetType().Name == "SilentLogger")
        {
            var loggerConfiguration = new LoggerConfiguration();
            ConfigureLogger(loggerConfiguration, configuration);
            Log.Logger = loggerConfiguration.CreateLogger();
        }

        return services;
    }

    /// <summary>
    /// Configures the logger with all sinks and enrichers
    /// </summary>
    private static void ConfigureLogger(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "Normaize")
            .Enrich.WithProperty("Version", GetVersion())
            .Enrich.With<TraceContextEnricher>(); // Add OpenTelemetry trace context

        // Add Seq sink if configured
        var seqServerUrl = configuration["Seq:ServerUrl"];
        if (!string.IsNullOrWhiteSpace(seqServerUrl))
        {
            loggerConfiguration.WriteTo.Seq(
                serverUrl: seqServerUrl,
                restrictedToMinimumLevel: LogEventLevel.Information);
        }

        // Always write to console
        loggerConfiguration.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Write to file if configured
        var logFilePath = configuration["Serilog:WriteTo:File:Path"];
        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            loggerConfiguration.WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }
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

