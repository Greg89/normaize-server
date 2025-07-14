using Normaize.Core.Interfaces;
using Normaize.Core.Constants;
using Normaize.API.Configuration;
using Serilog;

namespace Normaize.API.Configuration;

public static class StartupConfiguration
{
    public static async Task ConfigureStartup(WebApplication app)
    {
        var shouldRunStartupChecks = ShouldRunStartupChecks();
        
        if (shouldRunStartupChecks)
        {
            await RunStartupChecks(app);
        }
        else
        {
            Log.Information("No database connection detected and not in production-like environment, skipping migrations and health checks");
        }
    }

    private static bool ShouldRunStartupChecks()
    {
        var hasDatabaseConnection = AppConfiguration.HasDatabaseConnection();
        var isProductionLike = AppConfiguration.IsProductionLike();
        var isContainerized = AppConfiguration.IsContainerized();

        return hasDatabaseConnection || isProductionLike || isContainerized;
    }

    private static async Task RunStartupChecks(WebApplication app)
    {
        try
        {
            Log.Information("Starting database setup and health verification...");
            using var scope = app.Services.CreateScope();
            
            await ApplyMigrations(scope);
            await PerformHealthCheck(scope);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during database setup and health verification");
            HandleStartupError();
        }
    }

    private static async Task ApplyMigrations(IServiceScope scope)
    {
        var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
        var migrationResult = await migrationService.ApplyMigrations();
        
        if (!migrationResult.Success)
        {
            Log.Error("Migration failed: {Error}", migrationResult.ErrorMessage);
            
            // Log specific migration error details
            if (migrationResult.ErrorMessage?.Contains("Unknown column") == true)
            {
                Log.Error("Database schema mismatch detected. This may indicate a failed or incomplete migration.");
            }
            
            HandleMigrationFailure();
        }
        else
        {
            Log.Information("Database migrations applied successfully");
        }
    }

    private static async Task PerformHealthCheck(IServiceScope scope)
    {
        var healthCheckService = scope.ServiceProvider.GetRequiredService<IHealthCheckService>();
        var healthResult = await healthCheckService.CheckReadinessAsync();
        
        if (!healthResult.IsHealthy)
        {
            Log.Error("Startup health check failed");
            foreach (var issue in healthResult.Issues)
            {
                Log.Error("Issue: {Issue}", issue);
            }
            
            HandleHealthCheckFailure();
        }
        else
        {
            Log.Information("Startup health check passed - all systems healthy");
        }
    }

    private static void HandleMigrationFailure()
    {
        var currentEnvironment = AppConfiguration.GetEnvironment();
        if (IsProductionEnvironment(currentEnvironment))
        {
            Log.Fatal("Database migration failed in production environment. Application will not start.");
            throw new InvalidOperationException("Database migration failed in production environment");
        }
        else
        {
            Log.Warning("Migration failed but continuing in Development mode");
        }
    }

    private static void HandleHealthCheckFailure()
    {
        var currentEnvironment = AppConfiguration.GetEnvironment();
        if (IsProductionEnvironment(currentEnvironment))
        {
            Log.Fatal("Startup health check failed in production environment. Application will not start.");
            throw new InvalidOperationException("Startup health check failed in production environment");
        }
        else
        {
            Log.Warning("Startup health check failed but continuing in Development mode");
        }
    }

    private static void HandleStartupError()
    {
        var currentEnvironment = AppConfiguration.GetEnvironment();
        if (IsProductionEnvironment(currentEnvironment))
        {
            Log.Fatal("Database setup failed in production environment. Application will not start.");
            throw new InvalidOperationException("Database setup failed in production environment");
        }
        else
        {
            Log.Warning("Database setup failed but continuing in Development mode");
        }
    }

    private static bool IsProductionEnvironment(string environment) =>
        environment == AppConstants.Environment.PRODUCTION || 
        environment == AppConstants.Environment.STAGING;
} 