using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using System.Diagnostics;

namespace Normaize.Tests.Chaos;

/// <summary>
/// Template for implementing chaos engineering tests.
/// 
/// Chaos tests validate system resilience by simulating failure scenarios
/// and ensuring the system can handle them gracefully.
/// 
/// Usage:
/// 1. Inherit from this class
/// 2. Override the abstract methods
/// 3. Implement specific chaos scenarios
/// 4. Add to the chaos test suite
/// </summary>
public abstract class ChaosTestTemplate
{
    protected readonly ILogger<ChaosTestTemplate> _logger;
    protected readonly IMetricsService _metricsService;
    protected readonly ITracingService _tracingService;
    protected readonly IHealthCheckService _healthCheckService;
    
    protected abstract string TestName { get; }
    protected abstract string TestDescription { get; }
    protected abstract TimeSpan TestDuration { get; }
    protected abstract string[] AffectedServices { get; }
    
    public ChaosTestTemplate(
        ILogger<ChaosTestTemplate> logger,
        IMetricsService metricsService,
        ITracingService tracingService,
        IHealthCheckService healthCheckService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
    }
    
    /// <summary>
    /// Main entry point for running the chaos test.
    /// </summary>
    public async Task<ChaosTestResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting chaos test: {TestName}. CorrelationId: {CorrelationId}, Duration: {Duration}", 
            TestName, correlationId, TestDuration);
        
        using var span = _tracingService.StartSpan($"chaos_test.{TestName.ToLowerInvariant()}", new Dictionary<string, string>
        {
            ["test_name"] = TestName,
            ["correlation_id"] = correlationId,
            ["duration_minutes"] = TestDuration.TotalMinutes.ToString()
        });
        
        try
        {
            // Pre-test validation
            await PreTestValidationAsync(correlationId, cancellationToken);
            
            // Pre-test setup
            await PreTestSetupAsync(correlationId, cancellationToken);
            
            // Execute chaos scenario
            await ExecuteChaosScenarioAsync(correlationId, cancellationToken);
            
            // Monitor system during chaos
            var monitoringResults = await MonitorSystemDuringChaosAsync(correlationId, cancellationToken);
            
            // Post-test validation
            await PostTestValidationAsync(correlationId, cancellationToken);
            
            // Post-test cleanup
            await PostTestCleanupAsync(correlationId, cancellationToken);
            
            stopwatch.Stop();
            
            // Analyze results
            var result = AnalyzeResults(monitoringResults, stopwatch.Elapsed, correlationId);
            
            // Record metrics
            RecordTestMetrics(result, correlationId);
            
            _logger.LogInformation("Chaos test completed: {TestName}. Result: {Result}. Duration: {Duration}ms. CorrelationId: {CorrelationId}", 
                TestName, result.Status, stopwatch.ElapsedMilliseconds, correlationId);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Chaos test failed: {TestName}. Duration: {Duration}ms. CorrelationId: {CorrelationId}", 
                TestName, stopwatch.ElapsedMilliseconds, correlationId);
            
            // Record failure metrics
            _metricsService.IncrementCounter("chaos_test_failure", new Dictionary<string, string>
            {
                ["test_name"] = TestName,
                ["error_type"] = ex.GetType().Name,
                ["correlation_id"] = correlationId
            });
            
            return new ChaosTestResult
            {
                TestName = TestName,
                Status = ChaosTestStatus.Failed,
                CorrelationId = correlationId,
                Duration = stopwatch.Elapsed,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }
    
    /// <summary>
    /// Validate that the system is in a healthy state before running the test.
    /// </summary>
    protected virtual async Task PreTestValidationAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing pre-test validation. CorrelationId: {CorrelationId}", correlationId);
        
        var healthStatus = await _healthCheckService.GetHealthStatusAsync(cancellationToken);
        
        if (!healthStatus.IsHealthy)
        {
            throw new InvalidOperationException($"System is not healthy before chaos test. Status: {healthStatus.Status}");
        }
        
        _logger.LogDebug("Pre-test validation passed. CorrelationId: {CorrelationId}", correlationId);
    }
    
    /// <summary>
    /// Setup required for the chaos test (e.g., backup data, prepare monitoring).
    /// </summary>
    protected virtual async Task PreTestSetupAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing pre-test setup. CorrelationId: {CorrelationId}", correlationId);
        
        // Override in derived classes for specific setup
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Execute the actual chaos scenario (e.g., stop service, introduce latency).
    /// </summary>
    protected abstract Task ExecuteChaosScenarioAsync(string correlationId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Monitor system behavior during the chaos scenario.
    /// </summary>
    protected virtual async Task<ChaosMonitoringResult> MonitorSystemDuringChaosAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting system monitoring during chaos. CorrelationId: {CorrelationId}", correlationId);
        
        var monitoringResults = new ChaosMonitoringResult
        {
            HealthChecks = new List<HealthCheckSnapshot>(),
            Metrics = new List<MetricsSnapshot>(),
            Timestamps = new List<DateTime>()
        };
        
        var monitoringDuration = TestDuration;
        var checkInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds
        var totalChecks = (int)(monitoringDuration.TotalSeconds / checkInterval.TotalSeconds);
        
        for (int i = 0; i < totalChecks; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
            var timestamp = DateTime.UtcNow;
            
            // Collect health check
            var healthStatus = await _healthCheckService.GetHealthStatusAsync(cancellationToken);
            monitoringResults.HealthChecks.Add(new HealthCheckSnapshot
            {
                Timestamp = timestamp,
                Status = healthStatus.Status,
                IsHealthy = healthStatus.IsHealthy,
                Details = healthStatus.Details
            });
            
            // Collect metrics
            var metrics = await CollectMetricsAsync(correlationId);
            monitoringResults.Metrics.Add(new MetricsSnapshot
            {
                Timestamp = timestamp,
                Metrics = metrics
            });
            
            monitoringResults.Timestamps.Add(timestamp);
            
            _logger.LogDebug("Monitoring check {Check}/{Total}: Health={Health}, CorrelationId={CorrelationId}", 
                i + 1, totalChecks, healthStatus.Status, correlationId);
            
            // Wait for next check
            if (i < totalChecks - 1)
            {
                await Task.Delay(checkInterval, cancellationToken);
            }
        }
        
        return monitoringResults;
    }
    
    /// <summary>
    /// Validate that the system recovered properly after the chaos scenario.
    /// </summary>
    protected virtual async Task PostTestValidationAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing post-test validation. CorrelationId: {CorrelationId}", correlationId);
        
        // Wait a bit for system to stabilize
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        
        var healthStatus = await _healthCheckService.GetHealthStatusAsync(cancellationToken);
        
        if (!healthStatus.IsHealthy)
        {
            _logger.LogWarning("System did not fully recover after chaos test. Status: {Status}. CorrelationId: {CorrelationId}", 
                healthStatus.Status, correlationId);
        }
        else
        {
            _logger.LogDebug("System recovered successfully after chaos test. CorrelationId: {CorrelationId}", correlationId);
        }
    }
    
    /// <summary>
    /// Cleanup after the chaos test (e.g., restore services, cleanup data).
    /// </summary>
    protected virtual async Task PostTestCleanupAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing post-test cleanup. CorrelationId: {CorrelationId}", correlationId);
        
        // Override in derived classes for specific cleanup
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Collect system metrics during monitoring.
    /// </summary>
    protected virtual async Task<Dictionary<string, object>> CollectMetricsAsync(string correlationId)
    {
        var metrics = new Dictionary<string, object>();
        
        try
        {
            // Add basic system metrics
            metrics["cpu_usage"] = GetCpuUsage();
            metrics["memory_usage"] = GetMemoryUsage();
            metrics["active_requests"] = GetActiveRequestCount();
            
            // Add custom metrics specific to the test
            var customMetrics = await CollectCustomMetricsAsync(correlationId);
            foreach (var metric in customMetrics)
            {
                metrics[metric.Key] = metric.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect some metrics. CorrelationId: {CorrelationId}", correlationId);
        }
        
        return metrics;
    }
    
    /// <summary>
    /// Collect custom metrics specific to this chaos test.
    /// </summary>
    protected virtual async Task<Dictionary<string, object>> CollectCustomMetricsAsync(string correlationId)
    {
        // Override in derived classes for specific metrics
        return new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Analyze the monitoring results to determine test outcome.
    /// </summary>
    protected virtual ChaosTestResult AnalyzeResults(ChaosMonitoringResult monitoringResults, TimeSpan duration, string correlationId)
    {
        var totalChecks = monitoringResults.HealthChecks.Count;
        var healthyChecks = monitoringResults.HealthChecks.Count(h => h.IsHealthy);
        var unhealthyChecks = totalChecks - healthyChecks;
        
        var healthPercentage = totalChecks > 0 ? (double)healthyChecks / totalChecks : 1.0;
        
        ChaosTestStatus status;
        string summary;
        
        if (healthPercentage >= 0.9)
        {
            status = ChaosTestStatus.Passed;
            summary = $"System remained healthy throughout the test ({healthPercentage:P0} healthy)";
        }
        else if (healthPercentage >= 0.5)
        {
            status = ChaosTestStatus.Degraded;
            summary = $"System experienced temporary degradation but recovered ({healthPercentage:P0} healthy)";
        }
        else
        {
            status = ChaosTestStatus.Failed;
            summary = $"System failed to maintain health during the test ({healthPercentage:P0} healthy)";
        }
        
        return new ChaosTestResult
        {
            TestName = TestName,
            Status = status,
            CorrelationId = correlationId,
            Duration = duration,
            Summary = summary,
            MonitoringResults = monitoringResults,
            Timestamp = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Record metrics about the test execution.
    /// </summary>
    protected virtual void RecordTestMetrics(ChaosTestResult result, string correlationId)
    {
        _metricsService.IncrementCounter("chaos_test_execution", new Dictionary<string, string>
        {
            ["test_name"] = TestName,
            ["status"] = result.Status.ToString(),
            ["correlation_id"] = correlationId
        });
        
        _metricsService.RecordTimer("chaos_test_duration_seconds", 
            result.Duration,
            new Dictionary<string, string>
            {
                ["test_name"] = TestName,
                ["status"] = result.Status.ToString()
            });
    }
    
    // Helper methods for collecting system metrics
    private double GetCpuUsage()
    {
        // Implement CPU usage collection
        return 0.0;
    }
    
    private double GetMemoryUsage()
    {
        // Implement memory usage collection
        return 0.0;
    }
    
    private int GetActiveRequestCount()
    {
        // Implement active request count collection
        return 0;
    }
}

// Supporting classes

public enum ChaosTestStatus
{
    Passed,
    Degraded,
    Failed
}

public class ChaosTestResult
{
    public string TestName { get; set; } = string.Empty;
    public ChaosTestStatus Status { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Error { get; set; }
    public ChaosMonitoringResult? MonitoringResults { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ChaosMonitoringResult
{
    public List<HealthCheckSnapshot> HealthChecks { get; set; } = new();
    public List<MetricsSnapshot> Metrics { get; set; } = new();
    public List<DateTime> Timestamps { get; set; } = new();
}

public class HealthCheckSnapshot
{
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

// Example implementation

/// <summary>
/// Example chaos test that simulates database connection failure.
/// </summary>
public class DatabaseConnectionFailureTest : ChaosTestTemplate
{
    protected override string TestName => "Database Connection Failure";
    protected override string TestDescription => "Simulates database connection failure and validates system degradation";
    protected override TimeSpan TestDuration => TimeSpan.FromMinutes(5);
    protected override string[] AffectedServices => new[] { "Database", "DataProcessing" };
    
    public DatabaseConnectionFailureTest(
        ILogger<ChaosTestTemplate> logger,
        IMetricsService metricsService,
        ITracingService tracingService,
        IHealthCheckService healthCheckService)
        : base(logger, metricsService, tracingService, healthCheckService)
    {
    }
    
    protected override async Task ExecuteChaosScenarioAsync(string correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulating database connection failure. CorrelationId: {CorrelationId}", correlationId);
        
        // In a real implementation, you might:
        // 1. Stop the database service
        // 2. Block database network traffic
        // 3. Corrupt database connection strings
        // 4. Overload the database with requests
        
        // For this example, we'll just log the simulation
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); // Simulate some time for the failure to propagate
    }
    
    protected override async Task<Dictionary<string, object>> CollectCustomMetricsAsync(string correlationId)
    {
        var metrics = new Dictionary<string, object>();
        
        // Add database-specific metrics
        metrics["database_connection_count"] = 0; // Simulate no connections
        metrics["database_query_timeout_count"] = 10; // Simulate timeouts
        metrics["database_fallback_activated"] = true; // Simulate fallback
        
        return metrics;
    }
} 