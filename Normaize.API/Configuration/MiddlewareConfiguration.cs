using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.API.Middleware;

namespace Normaize.API.Configuration;

public static class MiddlewareConfiguration
{
    public static void ConfigureMiddleware(WebApplication app)
    {
        var loggingService = app.Services.GetService<IStructuredLoggingService>();
        
        try
        {
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
        try
        {
            var isDevelopment = app.Environment.IsDevelopment();
            var isBeta = app.Environment.EnvironmentName.Equals(AppConstants.Environment.BETA, StringComparison.OrdinalIgnoreCase);
            
            if (isDevelopment || isBeta)
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
                loggingService?.LogUserAction("Swagger skipped", new { Environment = app.Environment.EnvironmentName, Enabled = false });
            }
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Swagger configuration failed");
            throw;
        }
    }

    private static void ConfigureForwardedHeaders(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
        {
            app.UseForwardedHeaders();
            loggingService?.LogUserAction("Forwarded headers configured");
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Forwarded headers configuration failed");
            throw;
        }
    }

    private static void ConfigureHttpsRedirection(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
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
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "HTTPS redirection configuration failed");
            throw;
        }
    }

    private static void ConfigureCors(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
        {
            // Get the environment to determine which CORS policy to use
            var appConfigService = app.Services.GetService<IAppConfigurationService>();
            string environment = "Development"; // Default fallback
            
            try
            {
                environment = appConfigService?.GetEnvironment() ?? "Development";
            }
            catch (Exception ex)
            {
                loggingService?.LogException(ex, "Failed to get environment from configuration service, using default");
                // Continue with default environment
            }
            
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                app.UseCors("Development");
                loggingService?.LogUserAction("CORS configured with Development policy", new { Environment = environment, Policy = "Development" });
            }
            else if (environment.Equals("Beta", StringComparison.OrdinalIgnoreCase))
            {
                app.UseCors("Beta");
                loggingService?.LogUserAction("CORS configured with Beta policy", new { Environment = environment, Policy = "Beta" });
            }
            else
            {
                app.UseCors("Restrictive");
                loggingService?.LogUserAction("CORS configured with Restrictive policy", new { Environment = environment, Policy = "Restrictive" });
            }
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "CORS configuration failed");
            throw;
        }
    }

    private static void ConfigureAuthentication(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAuth0();
            loggingService?.LogUserAction("Authentication and authorization configured");
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Authentication configuration failed");
            throw;
        }
    }

    private static void ConfigureRequestLogging(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
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
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Request logging configuration failed");
            throw;
        }
    }

    private static void ConfigureControllers(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
        {
            app.MapControllers();
            loggingService?.LogUserAction("Controller mapping configured");
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Controller configuration failed");
            throw;
        }
    }

    private static void ConfigureHealthChecks(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
        {
            app.MapHealthChecks("/health/readiness");
            loggingService?.LogUserAction("Health checks configured", new { Endpoint = "/health/readiness" });
        }
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Health checks configuration failed");
            throw;
        }
    }

    private static void ConfigureExceptionHandling(WebApplication app, IStructuredLoggingService? loggingService)
    {
        try
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
        catch (Exception ex)
        {
            loggingService?.LogException(ex, "Exception handling configuration failed");
            throw;
        }
    }
} 