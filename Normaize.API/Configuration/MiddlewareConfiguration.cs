using Normaize.Core.Constants;
using Normaize.API.Middleware;

namespace Normaize.API.Configuration;

public static class MiddlewareConfiguration
{
    public static void ConfigureMiddleware(WebApplication app)
    {
        ConfigureSwagger(app);
        ConfigureForwardedHeaders(app);
        ConfigureHttpsRedirection(app);
        ConfigureCors(app);
        ConfigureAuthentication(app);
        ConfigureRequestLogging(app);
        ConfigureControllers(app);
        ConfigureHealthChecks(app);
        ConfigureExceptionHandling(app);
    }

    private static void ConfigureSwagger(WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals(AppConstants.Environment.BETA, StringComparison.OrdinalIgnoreCase))
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Normaize API v1");
                c.RoutePrefix = "swagger";
            });
        }
    }

    private static void ConfigureForwardedHeaders(WebApplication app)
    {
        app.UseForwardedHeaders();
    }

    private static void ConfigureHttpsRedirection(WebApplication app)
    {
        if (app.Environment.IsProduction() || app.Environment.IsEnvironment(AppConstants.Environment.BETA))
        {
            // In production (Railway), HTTPS is handled by the load balancer
            // Only redirect if we're not behind a proxy and have HTTPS configured
            var appConfigService = app.Services.GetService<Normaize.Core.Interfaces.IAppConfigurationService>();
            var httpsPort = appConfigService?.GetHttpsPort();
            if (!string.IsNullOrEmpty(httpsPort) && int.TryParse(httpsPort, out _))
            {
                app.UseHttpsRedirection();
            }
        }
        else
        {
            // In development, always use HTTPS redirection
            app.UseHttpsRedirection();
        }
    }

    private static void ConfigureCors(WebApplication app)
    {
        app.UseCors("AllowAll");
    }

    private static void ConfigureAuthentication(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAuth0();
    }

    private static void ConfigureRequestLogging(WebApplication app)
    {
        // Add request logging middleware (skip in test environment)
        if (!app.Environment.EnvironmentName.Equals(AppConstants.Environment.TEST, StringComparison.OrdinalIgnoreCase))
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }

    private static void ConfigureControllers(WebApplication app)
    {
        app.MapControllers();
    }

    private static void ConfigureHealthChecks(WebApplication app)
    {
        app.MapHealthChecks("/health/readiness");
    }

    private static void ConfigureExceptionHandling(WebApplication app)
    {
        // Global exception handler (skip in test environment)
        if (!app.Environment.EnvironmentName.Equals(AppConstants.Environment.TEST, StringComparison.OrdinalIgnoreCase))
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
} 