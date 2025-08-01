using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.API.Middleware;

namespace Normaize.API.Configuration;

public static class MiddlewareConfiguration
{
    public static void ConfigureMiddleware(WebApplication app)
    {
        IStructuredLoggingService? loggingService = null;

        try
        {
            // Create a scope to resolve scoped services
            using var scope = app.Services.CreateScope();
            loggingService = scope.ServiceProvider.GetService<IStructuredLoggingService>();

            loggingService?.LogUserAction("Middleware configuration started", new { Environment = app.Environment.EnvironmentName });

            ConfigureSwagger(app, loggingService);
            ConfigureForwardedHeaders(app, loggingService);
            ConfigureHttpsRedirection(app, loggingService);
            ConfigureCors(app, loggingService);
            ConfigureAuthentication(app, loggingService);
            ConfigureRequestLogging(app, loggingService);
            ConfigureControllers(app, loggingService);
            ConfigureHealthChecks(app, loggingService);
            ConfigureExceptionHandling(app, loggingService);

            loggingService?.LogUserAction("Middleware configuration completed successfully", new { Environment = app.Environment.EnvironmentName });
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Middleware configuration failed");
            throw; // Re-throw to fail fast if middleware configuration fails
        }
    }

    private static void ConfigureSwagger(WebApplication app, IStructuredLoggingService? loggingService)
    {
        var isDevelopment = app.Environment.IsDevelopment();

        if (isDevelopment)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Normaize API v1");
                c.RoutePrefix = "swagger";
            });

            loggingService?.LogUserAction("Swagger configured", new { Environment = app.Environment.EnvironmentName, Enabled = true });
        }
        else
        {
            loggingService?.LogUserAction("Swagger disabled", new { Environment = app.Environment.EnvironmentName, Enabled = false });
        }
    }

    private static void ConfigureForwardedHeaders(WebApplication app, IStructuredLoggingService? loggingService)
    {
        app.UseForwardedHeaders();
        loggingService?.LogUserAction("Forwarded headers configured");
    }

    private static void ConfigureHttpsRedirection(WebApplication app, IStructuredLoggingService? loggingService)
    {
        var isProduction = app.Environment.IsProduction();
        var isBeta = app.Environment.IsEnvironment(AppConstants.Environment.BETA);

        if (isProduction || isBeta)
        {
            // In production (Railway), HTTPS is handled by the load balancer
            // Only redirect if we're not behind a proxy and have HTTPS configured
            var appConfigService = app.Services.GetService<IAppConfigurationService>();
            string? httpsPort = null;

            try
            {
                httpsPort = appConfigService?.GetHttpsPort();
            }
            catch (Exception ex)
            {
                loggingService?.LogException(ex, "Failed to get HTTPS port from configuration service");
                // Continue without HTTPS redirection if we can't get the port
            }

            if (!string.IsNullOrEmpty(httpsPort) && int.TryParse(httpsPort, out _))
            {
                app.UseHttpsRedirection();
                loggingService?.LogUserAction("HTTPS redirection configured for production", new { HttpsPort = httpsPort });
            }
            else
            {
                loggingService?.LogUserAction("HTTPS redirection skipped - no valid port configured", new { HttpsPort = httpsPort });
            }
        }
        else
        {
            // In development, always use HTTPS redirection
            app.UseHttpsRedirection();
            loggingService?.LogUserAction("HTTPS redirection configured for development");
        }
    }

    private static void ConfigureCors(WebApplication app, IStructuredLoggingService? loggingService)
    {
        // Get the environment to determine which CORS policy to use
        var appConfigService = app.Services.GetService<IAppConfigurationService>();
        string environment = AppConstants.Environment.DEVELOPMENT; // Default fallback

        try
        {
            environment = appConfigService?.GetEnvironment() ?? AppConstants.Environment.DEVELOPMENT;
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Failed to get environment from configuration service, using default");
            // Continue with default environment
        }

        if (environment.Equals(AppConstants.Environment.DEVELOPMENT, StringComparison.OrdinalIgnoreCase))
        {
            app.UseCors(AppConstants.Environment.DEVELOPMENT);
            loggingService?.LogUserAction("CORS configured with Development policy", new { Environment = environment, Policy = AppConstants.Environment.DEVELOPMENT });
        }
        else if (environment.Equals(AppConstants.Environment.BETA, StringComparison.OrdinalIgnoreCase))
        {
            app.UseCors(AppConstants.Environment.BETA);
            loggingService?.LogUserAction("CORS configured with Beta policy", new { Environment = environment, Policy = AppConstants.Environment.BETA });
        }
        else
        {
            app.UseCors("Production");
            loggingService?.LogUserAction("CORS configured with Production policy", new { Environment = environment, Policy = "Production" });
        }
    }

    private static void ConfigureAuthentication(WebApplication app, IStructuredLoggingService? loggingService)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAuth0();
        loggingService?.LogUserAction("Authentication and authorization configured");
    }

    private static void ConfigureRequestLogging(WebApplication app, IStructuredLoggingService? loggingService)
    {
        // Add request logging middleware (skip in test environment)
        if (!app.Environment.EnvironmentName.Equals(AppConstants.Environment.TEST, StringComparison.OrdinalIgnoreCase))
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
            loggingService?.LogUserAction("Request logging middleware configured", new { Environment = app.Environment.EnvironmentName });
        }
        else
        {
            loggingService?.LogUserAction("Request logging middleware skipped for test environment");
        }
    }

    private static void ConfigureControllers(WebApplication app, IStructuredLoggingService? loggingService)
    {
        app.MapControllers();
        loggingService?.LogUserAction("Controller mapping configured");
    }

    private static void ConfigureHealthChecks(WebApplication app, IStructuredLoggingService? loggingService)
    {
        app.MapHealthChecks("/health/readiness");
        loggingService?.LogUserAction("Health checks configured", new { Endpoint = "/health/readiness" });
    }

    private static void ConfigureExceptionHandling(WebApplication app, IStructuredLoggingService? loggingService)
    {
        // Global exception handler (skip in test environment)
        if (!app.Environment.EnvironmentName.Equals(AppConstants.Environment.TEST, StringComparison.OrdinalIgnoreCase))
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            loggingService?.LogUserAction("Global exception handling configured", new { Environment = app.Environment.EnvironmentName });
        }
        else
        {
            loggingService?.LogUserAction("Global exception handling skipped for test environment");
        }
    }
}