using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Normaize.Data.Services;

/// <summary>
/// Service responsible for managing application startup operations including database migrations,
/// health checks, and configuration validation with comprehensive retry logic and environment awareness.
/// </summary>
/// <remarks>
/// This service orchestrates the startup sequence for the Normaize application, ensuring all
/// critical components are properly initialized before the application becomes ready to serve traffic.
/// It includes sophisticated retry mechanisms, environment-specific behavior, and comprehensive
/// logging for troubleshooting startup issues.
/// 
/// Key Features:
/// - Environment-aware startup checks (Development, Production, Container)
/// - Database migration management with timeout and retry logic
/// - Health check orchestration with parallel/sequential execution options
/// - Exponential backoff retry with jitter for resilience
/// - Comprehensive correlation ID tracking for distributed tracing
/// - Production vs. non-production error handling strategies
/// 
/// The service is designed to be called during application startup (typically in Program.cs)
/// and will block until all startup operations complete or fail according to environment rules.
/// </remarks>
public class StartupService : IStartupService
{
    #region Private Fields

    private readonly ILogger<StartupService> _logger;
    private readonly IHealthCheckService _healthCheckService;
    private readonly IMigrationService _migrationService;
    private readonly StartupConfigurationOptions _startupConfig;
    private readonly IAppConfigurationService _appConfigService;
    private readonly Random _random = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the StartupService with required dependencies.
    /// </summary>
    /// <param name="logger">Logger for recording startup operations and errors</param>
    /// <param name="healthCheckService">Service for performing health checks</param>
    /// <param name="migrationService">Service for managing database migrations</param>
    /// <param name="startupConfig">Configuration options for startup behavior</param>
    /// <param name="appConfigService">Service for application configuration and environment detection</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when startup configuration validation fails</exception>
    public StartupService(
        ILogger<StartupService> logger,
        IHealthCheckService healthCheckService,
        IMigrationService migrationService,
        IOptions<StartupConfigurationOptions> startupConfig,
        IAppConfigurationService appConfigService)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(healthCheckService, nameof(healthCheckService));
        ArgumentNullException.ThrowIfNull(migrationService, nameof(migrationService));
        ArgumentNullException.ThrowIfNull(startupConfig, nameof(startupConfig));
        ArgumentNullException.ThrowIfNull(appConfigService, nameof(appConfigService));

        _logger = logger;
        _healthCheckService = healthCheckService;
        _migrationService = migrationService;
        _startupConfig = startupConfig.Value;
        _appConfigService = appConfigService;

        ValidateConfiguration();
        LogConfiguration();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Configures and runs startup checks including database migrations and health checks
    /// based on environment configuration and current system state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the startup operation</returns>
    /// <remarks>
    /// This method orchestrates the complete startup sequence:
    /// 1. Determines if startup checks should run based on environment and configuration
    /// 2. Executes database migrations with retry logic and timeout handling
    /// 3. Performs health checks with retry logic and timeout handling
    /// 4. Handles errors according to environment-specific rules
    /// 
    /// The method includes comprehensive correlation ID tracking for distributed tracing
    /// and will either complete successfully or fail according to production vs. non-production rules.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when startup configuration fails in production environment</exception>
    public async Task ConfigureStartupAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GenerateCorrelationId();
        _logger.LogInformation("Starting startup configuration. {CorrelationIdLogProperty}: {CorrelationId}",
            AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);

        try
        {
            if (!ShouldRunStartupChecks())
            {
                _logger.LogInformation("Startup checks disabled for current environment. {CorrelationIdLogProperty}: {CorrelationId}",
    AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
                return;
            }

            await RunStartupChecksAsync(correlationId, cancellationToken);
            _logger.LogInformation("Startup configuration completed successfully. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Startup configuration was cancelled. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            throw new InvalidOperationException($"Startup configuration was cancelled. {AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY}: {correlationId}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup configuration. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            HandleStartupError(ex, correlationId);
        }
    }

    /// <summary>
    /// Determines whether startup checks should be run based on environment and configuration settings.
    /// </summary>
    /// <returns>True if startup checks should be run, false otherwise</returns>
    /// <remarks>
    /// Startup checks are enabled when any of the following conditions are met:
    /// - Database connection is available and database checks are enabled
    /// - Application is running in a container and container checks are enabled
    /// - Application is in a development environment and development checks are enabled
    /// 
    /// This allows for flexible startup behavior across different deployment scenarios.
    /// </remarks>
    public bool ShouldRunStartupChecks()
    {
        var environment = _appConfigService.GetEnvironment();
        var hasDatabaseConnection = _appConfigService.HasDatabaseConnection();
        var isContainerized = _appConfigService.IsContainerized();

        var shouldRun = _startupConfig.Environment.EnableStartupChecksWithDatabase && hasDatabaseConnection ||
                       _startupConfig.Environment.EnableStartupChecksInContainer && isContainerized ||
                       _startupConfig.Environment.EnableStartupChecksInDevelopment && IsDevelopmentEnvironment(environment);

        _logger.LogDebug("Startup checks decision: {EnvironmentLogProperty}={Environment}, HasDatabase={HasDatabase}, IsContainerized={IsContainerized}, ShouldRun={ShouldRun}",
            AppConstants.StartupService.ENVIRONMENT_LOG_PROPERTY, environment, hasDatabaseConnection, isContainerized, shouldRun);

        return shouldRun;
    }

    /// <summary>
    /// Applies database migrations with comprehensive retry logic, timeout handling, and error management.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the migration operation</returns>
    /// <remarks>
    /// This method applies database migrations with the following features:
    /// - Configurable timeout based on startup configuration
    /// - Retry logic with exponential backoff and jitter
    /// - Environment-specific error handling (fail-fast in production)
    /// - Comprehensive logging with correlation ID tracking
    /// - Proper cancellation token support
    /// 
    /// The method will either complete successfully or fail according to environment rules,
    /// ensuring data integrity in production environments.
    /// </remarks>
    /// <exception cref="TimeoutException">Thrown when migration operation exceeds configured timeout</exception>
    /// <exception cref="InvalidOperationException">Thrown when migration fails in production environment</exception>
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GenerateCorrelationId();
        _logger.LogInformation("Starting database migrations. {CorrelationIdLogProperty}: {CorrelationId}",
            AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);

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

            _logger.LogInformation("Database migrations applied successfully. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Database migration timed out after {TimeoutLogProperty} seconds. {CorrelationIdLogProperty}: {CorrelationId}",
                _startupConfig.Database.MigrationTimeoutSeconds, AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            throw new TimeoutException($"Database migration timed out after {_startupConfig.Database.MigrationTimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            HandleMigrationFailure(ex, correlationId);
        }
    }

    /// <summary>
    /// Performs startup health checks with comprehensive retry logic, timeout handling, and error management.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the health check operation</returns>
    /// <remarks>
    /// This method performs health checks with the following features:
    /// - Configurable timeout based on startup configuration
    /// - Retry logic with exponential backoff and jitter
    /// - Environment-specific error handling (fail-fast in production)
    /// - Comprehensive logging with correlation ID tracking
    /// - Proper cancellation token support
    /// 
    /// The method will either complete successfully or fail according to environment rules,
    /// ensuring system health in production environments.
    /// </remarks>
    /// <exception cref="TimeoutException">Thrown when health check operation exceeds configured timeout</exception>
    /// <exception cref="InvalidOperationException">Thrown when health check fails in production environment</exception>
    public async Task PerformHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = GenerateCorrelationId();
        _logger.LogInformation("Starting health checks. {CorrelationIdLogProperty}: {CorrelationId}",
            AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);

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

            _logger.LogInformation("Health checks passed successfully. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Health check timed out after {TimeoutLogProperty} seconds. {CorrelationIdLogProperty}: {CorrelationId}",
                _startupConfig.HealthCheck.HealthCheckTimeoutSeconds, AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            throw new TimeoutException($"Health check timed out after {_startupConfig.HealthCheck.HealthCheckTimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            HandleHealthCheckFailure(ex, correlationId);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Orchestrates the execution of startup checks based on configuration.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracking the operation</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the startup checks operation</returns>
    /// <remarks>
    /// This method determines whether to run migrations and health checks in parallel or sequentially
    /// based on the RunHealthChecksInParallel configuration option. Parallel execution can improve
    /// startup performance but may increase resource usage during startup.
    /// </remarks>
    private async Task RunStartupChecksAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running startup checks. {CorrelationIdLogProperty}: {CorrelationId}",
            AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);

        if (_startupConfig.HealthCheck.RunHealthChecksInParallel)
        {
            // Run migrations and health checks in parallel for improved startup performance
            var migrationTask = ApplyMigrationsAsync(cancellationToken);
            var healthCheckTask = PerformHealthChecksAsync(cancellationToken);

            await Task.WhenAll(migrationTask, healthCheckTask);
        }
        else
        {
            // Run migrations first, then health checks for sequential dependency management
            await ApplyMigrationsAsync(cancellationToken);
            await PerformHealthChecksAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Executes an operation with comprehensive retry logic including exponential backoff and jitter.
    /// </summary>
    /// <typeparam name="T">Type of the operation result</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="baseDelay">Base delay between retry attempts</param>
    /// <param name="operationName">Name of the operation for logging purposes</param>
    /// <param name="correlationId">Correlation ID for tracking the operation</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the operation result</returns>
    /// <remarks>
    /// This method implements sophisticated retry logic with the following features:
    /// - Configurable maximum retry attempts
    /// - Exponential backoff with configurable base delay
    /// - Jitter to prevent thundering herd problems
    /// - Proper cancellation token support
    /// - Comprehensive logging with correlation ID tracking
    /// - Exception preservation for debugging
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when operation fails after all retry attempts</exception>
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
                    _logger.LogError(ex, "{OperationLogProperty} failed after {AttemptLogProperty} attempts. {CorrelationIdLogProperty}: {CorrelationId}",
                        operationName, attempt, AppConstants.StartupService.ATTEMPT_LOG_PROPERTY, correlationId);
                    break;
                }

                var delay = CalculateDelay(attempt, baseDelay);
                _logger.LogWarning(ex, "{OperationLogProperty} failed (attempt {AttemptLogProperty}/{MaxAttemptsLogProperty}). Retrying in {DelayLogProperty}ms. {CorrelationIdLogProperty}: {CorrelationId}",
                    operationName, attempt, maxRetries + 1, delay.TotalMilliseconds, AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"{operationName} failed after {maxRetries + 1} attempts", lastException);
    }

    /// <summary>
    /// Calculates the delay for retry attempts with support for exponential backoff and jitter.
    /// </summary>
    /// <param name="attempt">Current attempt number (1-based)</param>
    /// <param name="baseDelay">Base delay between retry attempts</param>
    /// <returns>Calculated delay for the retry attempt</returns>
    /// <remarks>
    /// This method calculates retry delays with the following features:
    /// - Exponential backoff: delay = baseDelay * 2^(attempt-1)
    /// - Maximum delay cap to prevent excessive delays
    /// - Jitter to prevent synchronized retry attempts
    /// - Configurable jitter factor for fine-tuning
    /// 
    /// The jitter helps prevent multiple instances from retrying simultaneously,
    /// reducing the load on the target system during recovery.
    /// </remarks>
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

    /// <summary>
    /// Determines if the specified environment is a development environment.
    /// </summary>
    /// <param name="environment">Environment name to check</param>
    /// <returns>True if the environment is a development environment, false otherwise</returns>
    /// <remarks>
    /// This method performs case-insensitive comparison against the configured
    /// development environments list from startup configuration.
    /// </remarks>
    private bool IsDevelopmentEnvironment(string environment) =>
        _startupConfig.Environment.DevelopmentEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if the specified environment is a production environment.
    /// </summary>
    /// <param name="environment">Environment name to check</param>
    /// <returns>True if the environment is a production environment, false otherwise</returns>
    /// <remarks>
    /// This method performs case-insensitive comparison against the configured
    /// production environments list from startup configuration.
    /// </remarks>
    private bool IsProductionEnvironment(string environment) =>
        _startupConfig.Environment.ProductionEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase);

    #endregion

    #region Error Handling Methods

    /// <summary>
    /// Handles database migration failures according to environment-specific rules.
    /// </summary>
    /// <param name="ex">The exception that caused the migration failure</param>
    /// <param name="correlationId">Correlation ID for tracking the failure</param>
    /// <remarks>
    /// This method implements environment-specific error handling:
    /// - Production environments: Fail-fast if configured to do so
    /// - Non-production environments: Log warning and continue
    /// 
    /// The behavior is controlled by the FailOnMigrationError configuration option
    /// and the current environment detection.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when migration fails in production environment with fail-fast enabled</exception>
    private void HandleMigrationFailure(Exception ex, string correlationId)
    {
        var environment = _appConfigService.GetEnvironment();

        if (IsProductionEnvironment(environment) && _startupConfig.Database.FailOnMigrationError)
        {
            _logger.LogCritical(ex, "Database migration failed in production environment. Application will not start. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            throw new InvalidOperationException("Database migration failed in production environment", ex);
        }
        else
        {
            _logger.LogWarning(ex, "Database migration failed but continuing in non-production mode. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
        }
    }

    /// <summary>
    /// Handles health check failures according to environment-specific rules.
    /// </summary>
    /// <param name="ex">The exception that caused the health check failure</param>
    /// <param name="correlationId">Correlation ID for tracking the failure</param>
    /// <remarks>
    /// This method implements environment-specific error handling:
    /// - Production environments: Fail-fast if configured to do so
    /// - Non-production environments: Log warning and continue
    /// 
    /// The behavior is controlled by the FailOnHealthCheckError configuration option
    /// and the current environment detection.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when health check fails in production environment with fail-fast enabled</exception>
    private void HandleHealthCheckFailure(Exception ex, string correlationId)
    {
        var environment = _appConfigService.GetEnvironment();

        if (IsProductionEnvironment(environment) && _startupConfig.HealthCheck.FailOnHealthCheckError)
        {
            _logger.LogCritical(ex, "Health check failed in production environment. Application will not start. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            throw new InvalidOperationException("Health check failed in production environment", ex);
        }
        else
        {
            _logger.LogWarning(ex, "Health check failed but continuing in non-production mode. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
        }
    }

    /// <summary>
    /// Handles general startup errors according to environment-specific rules.
    /// </summary>
    /// <param name="ex">The exception that caused the startup failure</param>
    /// <param name="correlationId">Correlation ID for tracking the failure</param>
    /// <remarks>
    /// This method implements environment-specific error handling:
    /// - Production environments: Always fail-fast for critical startup errors
    /// - Non-production environments: Log warning and continue
    /// 
    /// Startup errors are considered critical and will always cause application
    /// startup to fail in production environments.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when startup fails in production environment</exception>
    private void HandleStartupError(Exception ex, string correlationId)
    {
        var environment = _appConfigService.GetEnvironment();

        if (IsProductionEnvironment(environment))
        {
            _logger.LogCritical(ex, "Startup configuration failed in production environment. Application will not start. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
            throw new InvalidOperationException("Startup configuration failed in production environment", ex);
        }
        else
        {
            _logger.LogWarning(ex, "Startup configuration failed but continuing in non-production mode. {CorrelationIdLogProperty}: {CorrelationId}",
                AppConstants.StartupService.CORRELATION_ID_LOG_PROPERTY, correlationId);
        }
    }

    #endregion

    #region Configuration Methods

    /// <summary>
    /// Validates the startup configuration using data annotations validation.
    /// </summary>
    /// <remarks>
    /// This method validates the startup configuration object using System.ComponentModel.DataAnnotations
    /// validation attributes. It ensures all required configuration sections are present and
    /// all configuration values are within acceptable ranges.
    /// 
    /// If validation fails, the method throws an InvalidOperationException with details
    /// about which validation rules were violated.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when configuration validation fails</exception>
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

    /// <summary>
    /// Logs the startup configuration for debugging and monitoring purposes.
    /// </summary>
    /// <remarks>
    /// This method logs key configuration values to help with debugging startup issues
    /// and monitoring configuration changes. It logs timeout values, retry settings,
    /// and other critical configuration parameters.
    /// 
    /// The logging is done at Information level to ensure visibility during startup
    /// while avoiding excessive log verbosity.
    /// </remarks>
    private void LogConfiguration()
    {
        _logger.LogInformation("StartupService initialized with configuration: MigrationTimeout={MigrationTimeout}s, HealthCheckTimeout={HealthCheckTimeout}s, MaxRetries={MaxRetries}, EnableExponentialBackoff={EnableExponentialBackoff}",
            _startupConfig.Database.MigrationTimeoutSeconds,
            _startupConfig.HealthCheck.HealthCheckTimeoutSeconds,
            _startupConfig.Retry.MaxRetries,
            _startupConfig.Retry.EnableExponentialBackoff);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Generates a unique correlation ID for tracking operations across the startup process.
    /// </summary>
    /// <returns>A unique correlation ID string</returns>
    /// <remarks>
    /// This method generates a new GUID for each operation to enable distributed tracing
    /// and correlation of related log entries. The correlation ID is used throughout
    /// the startup process to track the progress of individual startup operations.
    /// 
    /// The correlation ID format is a standard GUID string that can be easily
    /// parsed and used by logging and monitoring systems.
    /// </remarks>
    private static string GenerateCorrelationId() => Guid.NewGuid().ToString();

    #endregion
}