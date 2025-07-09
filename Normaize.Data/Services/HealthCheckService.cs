using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Data;
using System.Diagnostics;

namespace Normaize.Data.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly NormaizeContext _context;
    private readonly IDatabaseHealthService _databaseHealthService;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IStorageService _storageService;

    public HealthCheckService(
        NormaizeContext context,
        IDatabaseHealthService databaseHealthService,
        IStorageService storageService,
        ILogger<HealthCheckService> logger)
    {
        _context = context;
        _databaseHealthService = databaseHealthService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();
        var issues = new List<string>();

        try
        {
            _logger.LogInformation("Starting comprehensive health check...");

            // Check all components
            var components = new Dictionary<string, ComponentHealth>();

            // 1. Database Health
            var dbHealth = await CheckDatabaseHealthAsync();
            components["database"] = dbHealth;
            if (!dbHealth.IsHealthy)
            {
                issues.Add($"Database: {dbHealth.ErrorMessage}");
            }

            // 2. Application Health
            var appHealth = await CheckApplicationHealthAsync();
            components["application"] = appHealth;
            if (!appHealth.IsHealthy)
            {
                issues.Add($"Application: {appHealth.ErrorMessage}");
            }

            // 3. Storage Health
            var storageHealth = await CheckStorageHealthAsync();
            components["storage"] = storageHealth;
            if (!storageHealth.IsHealthy)
            {
                issues.Add($"Storage: {storageHealth.ErrorMessage}");
            }

            // 4. External Services Health
            var externalHealth = await CheckExternalServicesHealthAsync();
            components["external_services"] = externalHealth;
            if (!externalHealth.IsHealthy)
            {
                issues.Add($"External Services: {externalHealth.ErrorMessage}");
            }

            // 5. System Resources Health
            var systemHealth = await CheckSystemResourcesHealthAsync();
            components["system_resources"] = systemHealth;
            if (!systemHealth.IsHealthy)
            {
                issues.Add($"System Resources: {systemHealth.ErrorMessage}");
            }

            // Determine overall health
            result.IsHealthy = components.All(c => c.Value.IsHealthy);
            result.Status = result.IsHealthy ? "healthy" : "unhealthy";
            result.Components = components;
            result.Issues = issues;

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            if (result.IsHealthy)
            {
                _logger.LogInformation("Comprehensive health check completed successfully in {Duration}ms", result.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("Health check failed with {IssueCount} issues in {Duration}ms", issues.Count, result.Duration.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during health check");
            
            result.IsHealthy = false;
            result.Status = "error";
            result.Issues.Add($"Health check error: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            
            return result;
        }
    }

    public async Task<HealthCheckResult> CheckLivenessAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();

        try
        {
            // Liveness check - just verify the application is responsive
            var appHealth = await CheckApplicationHealthAsync();
            
            result.IsHealthy = appHealth.IsHealthy;
            result.Status = appHealth.IsHealthy ? "alive" : "not_alive";
            result.Components = new Dictionary<string, ComponentHealth>
            {
                ["application"] = appHealth
            };
            
            if (!appHealth.IsHealthy)
            {
                result.Issues.Add($"Application: {appHealth.ErrorMessage}");
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.Status = "error";
            result.Issues.Add($"Liveness check error: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<HealthCheckResult> CheckReadinessAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();

        try
        {
            // Readiness check - verify the app is ready to serve traffic
            var components = new Dictionary<string, ComponentHealth>();

            // Database connectivity
            var dbHealth = await CheckDatabaseHealthAsync();
            components["database"] = dbHealth;

            // Application health
            var appHealth = await CheckApplicationHealthAsync();
            components["application"] = appHealth;

            // Storage connectivity
            var storageHealth = await CheckStorageHealthAsync();
            components["storage"] = storageHealth;

            var issues = new List<string>();
            if (!dbHealth.IsHealthy) issues.Add($"Database: {dbHealth.ErrorMessage}");
            if (!appHealth.IsHealthy) issues.Add($"Application: {appHealth.ErrorMessage}");
            if (!storageHealth.IsHealthy) issues.Add($"Storage: {storageHealth.ErrorMessage}");

            result.IsHealthy = components.All(c => c.Value.IsHealthy);
            result.Status = result.IsHealthy ? "ready" : "not_ready";
            result.Components = components;
            result.Issues = issues;

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.Status = "error";
            result.Issues.Add($"Readiness check error: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task<ComponentHealth> CheckDatabaseHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var dbHealthResult = await _databaseHealthService.CheckHealthAsync();
            
            stopwatch.Stop();
            
            return new ComponentHealth
            {
                IsHealthy = dbHealthResult.IsHealthy,
                Status = dbHealthResult.Status,
                ErrorMessage = dbHealthResult.ErrorMessage,
                Details = new Dictionary<string, object>
                {
                    ["missingColumns"] = dbHealthResult.MissingColumns,
                    ["connectionString"] = "configured" // Don't expose actual connection string
                },
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<ComponentHealth> CheckApplicationHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Check if application is responsive
            var canConnect = await _context.Database.CanConnectAsync();
            
            // Check for pending migrations
            var pendingMigrations = _context.Database.GetPendingMigrations().ToList();
            
            stopwatch.Stop();
            
            var isHealthy = canConnect && !pendingMigrations.Any();
            
            return new ComponentHealth
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "healthy" : "unhealthy",
                ErrorMessage = !canConnect ? "Cannot connect to database" : 
                              pendingMigrations.Any() ? $"Pending migrations: {string.Join(", ", pendingMigrations)}" : null,
                Details = new Dictionary<string, object>
                {
                    ["pendingMigrations"] = pendingMigrations,
                    ["canConnect"] = canConnect,
                    ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                },
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<ComponentHealth> CheckStorageHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Test storage service connectivity
            var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER") ?? "memory";
            
            // For in-memory storage, just verify it's available
            if (storageProvider == "memory")
            {
                stopwatch.Stop();
                return new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Details = new Dictionary<string, object>
                    {
                        ["provider"] = storageProvider,
                        ["type"] = "in_memory"
                    },
                    Duration = stopwatch.Elapsed
                };
            }

            // For other storage providers, test connectivity
            // This would need to be implemented based on your storage service
            stopwatch.Stop();
            
            return new ComponentHealth
            {
                IsHealthy = true, // Assume healthy for now
                Status = "healthy",
                Details = new Dictionary<string, object>
                {
                    ["provider"] = storageProvider,
                    ["type"] = "external"
                },
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<ComponentHealth> CheckExternalServicesHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var issues = new List<string>();
            var details = new Dictionary<string, object>();

            // Check Auth0 configuration
            var auth0Issuer = Environment.GetEnvironmentVariable("AUTH0_ISSUER");
            var auth0Audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE");
            
            if (string.IsNullOrEmpty(auth0Issuer))
            {
                issues.Add("Auth0 issuer not configured");
            }
            if (string.IsNullOrEmpty(auth0Audience))
            {
                issues.Add("Auth0 audience not configured");
            }

            // Check Seq logging configuration
            var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL");
            if (!string.IsNullOrEmpty(seqUrl))
            {
                details["seq_logging"] = "configured";
            }

            stopwatch.Stop();
            
            return new ComponentHealth
            {
                IsHealthy = !issues.Any(),
                Status = !issues.Any() ? "healthy" : "unhealthy",
                ErrorMessage = issues.Any() ? string.Join("; ", issues) : null,
                Details = details,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<ComponentHealth> CheckSystemResourcesHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var details = new Dictionary<string, object>();
            var issues = new List<string>();

            // Check memory usage
            var memoryInfo = GC.GetGCMemoryInfo();
            var totalMemory = GC.GetTotalMemory(false);
            var maxMemory = memoryInfo.TotalAvailableMemoryBytes;
            var memoryUsagePercent = (double)totalMemory / maxMemory * 100;

            details["memory_usage_mb"] = Math.Round(totalMemory / 1024.0 / 1024.0, 2);
            details["memory_usage_percent"] = Math.Round(memoryUsagePercent, 2);
            details["max_memory_mb"] = Math.Round(maxMemory / 1024.0 / 1024.0, 2);

            if (memoryUsagePercent > 90)
            {
                issues.Add($"High memory usage: {Math.Round(memoryUsagePercent, 1)}%");
            }

            // Check disk space (if possible)
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/");
                var freeSpacePercent = (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100;
                
                details["disk_free_gb"] = Math.Round(driveInfo.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 2);
                details["disk_free_percent"] = Math.Round(freeSpacePercent, 2);

                if (freeSpacePercent < 10)
                {
                    issues.Add($"Low disk space: {Math.Round(freeSpacePercent, 1)}% free");
                }
            }
            catch
            {
                details["disk_space"] = "unavailable";
            }

            // Check thread pool
            ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
            details["available_worker_threads"] = workerThreads;
            details["available_completion_port_threads"] = completionPortThreads;

            stopwatch.Stop();
            
            return new ComponentHealth
            {
                IsHealthy = !issues.Any(),
                Status = !issues.Any() ? "healthy" : "unhealthy",
                ErrorMessage = issues.Any() ? string.Join("; ", issues) : null,
                Details = details,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }
} 