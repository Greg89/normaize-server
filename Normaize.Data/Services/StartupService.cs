using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Normaize.Data.Services;

public class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;
    private readonly IHealthCheckService _healthCheckService;
    private readonly IMigrationService _migrationService;
    private readonly StartupConfigurationOptions _startupConfig;
    private readonly IAppConfigurationService _appConfigService;
    private readonly Random _random = new();

    public StartupService(
        ILogger<StartupService> logger,
        IHealthCheckService healthCheckService,
        IMigrationService migrationService,
        IOptions<StartupConfigurationOptions> startupConfig,
        IAppConfigurationService appConfigService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(healthCheckService);
        ArgumentNullException.ThrowIfNull(migrationService);
        ArgumentNullException.ThrowIfNull(startupConfig);
        ArgumentNullException.ThrowIfNull(appConfigService);
        _logger = logger;
        _healthCheckService = healthCheckService;
        _migrationService = migrationService;
        _startupConfig = startupConfig.Value;
        _appConfigService = appConfigService;

        ValidateConfiguration();
        LogConfiguration();
    }

    public async Task ConfigureStartupAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GenerateCorrelationId();
        _logger.LogInformation("Starting startup configuration. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            if (!ShouldRunStartupChecks())
            {
                _logger.LogInformation("Startup checks disabled for current environment. CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            await RunStartupChecksAsync(correlationId, cancellationToken);
            _logger.LogInformation("Startup configuration completed successfully. CorrelationId: {CorrelationId}", correlationId);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Startup configuration was cancelled. CorrelationId: {CorrelationId}", correlationId);
            throw new InvalidOperationException($"Startup configuration was cancelled. CorrelationId: {correlationId}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup configuration. CorrelationId: {CorrelationId}", correlationId);
            HandleStartupError(ex, correlationId);
        }
    }

    public bool ShouldRunStartupChecks()
    {
        var environment = _appConfigService.GetEnvironment();
        var hasDatabaseConnection = _appConfigService.HasDatabaseConnection();
        var isContainerized = _appConfigService.IsContainerized();

        var shouldRun = _startupConfig.Environment.EnableStartupChecksWithDatabase && hasDatabaseConnection ||
                       _startupConfig.Environment.EnableStartupChecksInContainer && isContainerized ||
                       _startupConfig.Environment.EnableStartupChecksInDevelopment && IsDevelopmentEnvironment(environment);

        _logger.LogDebug("Startup checks decision: Environment={Environment}, HasDatabase={HasDatabase}, IsContainerized={IsContainerized}, ShouldRun={ShouldRun}",
            environment, hasDatabaseConnection, isContainerized, shouldRun);

        return shouldRun;
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GenerateCorrelationId();
        _logger.LogInformation("Starting database migrations. CorrelationId: {CorrelationId}", correlationId);

        var timeout = TimeSpan.FromSeconds(_startupConfig.Database.MigrationTimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await ExecuteWithRetryAsync(
                async () =>
                {
                    var result = await _migrationService.ApplyMigrations();
                    if (!result.Success)
                    {
                        throw new InvalidOperationException($"Migration failed: {result.ErrorMessage}");
                    }
                    return result;
                },
                _startupConfig.Database.MaxMigrationRetries,
                TimeSpan.FromSeconds(_startupConfig.Database.MigrationRetryDelaySeconds),
                "database migration",
                correlationId,
                cts.Token);

            _logger.LogInformation("Database migrations applied successfully. CorrelationId: {CorrelationId}", correlationId);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Database migration timed out after {Timeout} seconds. CorrelationId: {CorrelationId}", 
                _startupConfig.Database.MigrationTimeoutSeconds, correlationId);
            throw new TimeoutException($"Database migration timed out after {_startupConfig.Database.MigrationTimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed. CorrelationId: {CorrelationId}", correlationId);
            HandleMigrationFailure(ex, correlationId);
        }
    }

    public async Task PerformHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GenerateCorrelationId();
        _logger.LogInformation("Starting health checks. CorrelationId: {CorrelationId}", correlationId);

        var timeout = TimeSpan.FromSeconds(_startupConfig.HealthCheck.HealthCheckTimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await ExecuteWithRetryAsync(
                async () =>
                {
                    var result = await _healthCheckService.CheckReadinessAsync(cts.Token);
                    if (!result.IsHealthy)
                    {
                        var issues = string.Join("; ", result.Issues);
                        throw new InvalidOperationException($"Health check failed: {issues}");
                    }
                    return result;
                },
                _startupConfig.HealthCheck.MaxHealthCheckRetries,
                TimeSpan.FromSeconds(_startupConfig.HealthCheck.HealthCheckRetryDelaySeconds),
                "health check",
                correlationId,
                cts.Token);

            _logger.LogInformation("Health checks passed successfully. CorrelationId: {CorrelationId}", correlationId);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Health check timed out after {Timeout} seconds. CorrelationId: {CorrelationId}", 
                _startupConfig.HealthCheck.HealthCheckTimeoutSeconds, correlationId);
            throw new TimeoutException($"Health check timed out after {_startupConfig.HealthCheck.HealthCheckTimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed. CorrelationId: {CorrelationId}", correlationId);
            HandleHealthCheckFailure(ex, correlationId);
        }
    }

    private async Task RunStartupChecksAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running startup checks. CorrelationId: {CorrelationId}", correlationId);

        if (_startupConfig.HealthCheck.RunHealthChecksInParallel)
        {
            // Run migrations and health checks in parallel
            var migrationTask = ApplyMigrationsAsync(cancellationToken);
            var healthCheckTask = PerformHealthChecksAsync(cancellationToken);

            await Task.WhenAll(migrationTask, healthCheckTask);
        }
        else
        {
            // Run migrations first, then health checks
            await ApplyMigrationsAsync(cancellationToken);
            await PerformHealthChecksAsync(cancellationToken);
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries,
        TimeSpan baseDelay,
        string operationName,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt > maxRetries)
                {
                    _logger.LogError(ex, "{Operation} failed after {Attempts} attempts. CorrelationId: {CorrelationId}", 
                        operationName, attempt, correlationId);
                    break;
                }

                var delay = CalculateDelay(attempt, baseDelay);
                _logger.LogWarning(ex, "{Operation} failed (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms. CorrelationId: {CorrelationId}", 
                    operationName, attempt, maxRetries + 1, delay.TotalMilliseconds, correlationId);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"{operationName} failed after {maxRetries + 1} attempts", lastException);
    }

    private TimeSpan CalculateDelay(int attempt, TimeSpan baseDelay)
    {
        if (!_startupConfig.Retry.EnableExponentialBackoff)
        {
            return baseDelay;
        }

        var delayMs = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var maxDelayMs = baseDelay.TotalMilliseconds * _startupConfig.Retry.MaxDelayMultiplier;
        delayMs = Math.Min(delayMs, maxDelayMs);

        if (_startupConfig.Retry.EnableJitter)
        {
            var jitter = delayMs * _startupConfig.Retry.JitterFactor * (_random.NextDouble() * 2 - 1);
            delayMs += jitter;
        }

        return TimeSpan.FromMilliseconds(Math.Max(0, delayMs));
    }

    private bool IsDevelopmentEnvironment(string environment) =>
        _startupConfig.Environment.DevelopmentEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase);

    private bool IsProductionEnvironment(string environment) =>
        _startupConfig.Environment.ProductionEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase);

    private void HandleMigrationFailure(Exception ex, string correlationId)
    {
        var environment = _appConfigService.GetEnvironment();
        
        if (IsProductionEnvironment(environment) && _startupConfig.Database.FailOnMigrationError)
        {
            _logger.LogCritical(ex, "Database migration failed in production environment. Application will not start. CorrelationId: {CorrelationId}", correlationId);
            throw new InvalidOperationException("Database migration failed in production environment", ex);
        }
        else
        {
            _logger.LogWarning(ex, "Database migration failed but continuing in non-production mode. CorrelationId: {CorrelationId}", correlationId);
        }
    }

    private void HandleHealthCheckFailure(Exception ex, string correlationId)
    {
        var environment = _appConfigService.GetEnvironment();
        
        if (IsProductionEnvironment(environment) && _startupConfig.HealthCheck.FailOnHealthCheckError)
        {
            _logger.LogCritical(ex, "Health check failed in production environment. Application will not start. CorrelationId: {CorrelationId}", correlationId);
            throw new InvalidOperationException("Health check failed in production environment", ex);
        }
        else
        {
            _logger.LogWarning(ex, "Health check failed but continuing in non-production mode. CorrelationId: {CorrelationId}", correlationId);
        }
    }

    private void HandleStartupError(Exception ex, string correlationId)
    {
        var environment = _appConfigService.GetEnvironment();
        
        if (IsProductionEnvironment(environment))
        {
            _logger.LogCritical(ex, "Startup configuration failed in production environment. Application will not start. CorrelationId: {CorrelationId}", correlationId);
            throw new InvalidOperationException("Startup configuration failed in production environment", ex);
        }
        else
        {
            _logger.LogWarning(ex, "Startup configuration failed but continuing in non-production mode. CorrelationId: {CorrelationId}", correlationId);
        }
    }

    private void ValidateConfiguration()
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(_startupConfig);
        
        if (!Validator.TryValidateObject(_startupConfig, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Startup configuration validation failed: {errors}");
        }
    }

    private void LogConfiguration()
    {
        _logger.LogInformation("StartupService initialized with configuration: " +
            "MigrationTimeout={MigrationTimeout}s, HealthCheckTimeout={HealthCheckTimeout}s, " +
            "MaxRetries={MaxRetries}, EnableExponentialBackoff={EnableExponentialBackoff}",
            _startupConfig.Database.MigrationTimeoutSeconds,
            _startupConfig.HealthCheck.HealthCheckTimeoutSeconds,
            _startupConfig.Retry.MaxRetries,
            _startupConfig.Retry.EnableExponentialBackoff);
    }

    private static string GenerateCorrelationId() => Guid.NewGuid().ToString();
} 