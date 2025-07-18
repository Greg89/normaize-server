# Code Review Checklist - Logging & Chaos Engineering

## Pre-Review Checklist

### General Standards
- [ ] Code follows project naming conventions
- [ ] No hardcoded values or magic numbers
- [ ] Proper error handling implemented
- [ ] Unit tests written and passing
- [ ] Integration tests updated if needed

## Logging Standards

### ✅ Required Checks
- [ ] **NO log-and-rethrow patterns** - Exceptions should bubble up naturally
- [ ] **Correlation IDs included** in all log entries for request tracing
- [ ] **Structured logging** used with proper context objects
- [ ] **Appropriate log levels** used (Debug, Info, Warning, Error, Critical)
- [ ] **No sensitive data** logged (passwords, tokens, PII)
- [ ] **Consistent log format** across all services

### ✅ Logging Examples

#### ❌ BAD - Log and Rethrow
```csharp
try
{
    // Some operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    throw; // ❌ DON'T DO THIS
}
```

#### ✅ GOOD - Let Exceptions Bubble Up
```csharp
// Let exceptions bubble up to top-level handler
public async Task<string> ProcessDataAsync()
{
    // Operation logic without try-catch
    return await _dataService.ProcessAsync();
}
```

#### ✅ GOOD - Top-Level Exception Handling
```csharp
public static void ConfigureServices(WebApplicationBuilder builder)
{
    try
    {
        // All configuration methods
        ConfigureDatabase(builder);
        ConfigureCors(builder);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Service configuration failed");
        throw; // Single log point at top level
    }
}
```

#### ✅ GOOD - Structured Logging
```csharp
_logger.LogInformation("Processing file upload. CorrelationId: {CorrelationId}, FileName: {FileName}, Size: {Size}", 
    correlationId, fileName, fileSize);
```

## Chaos Engineering Standards

### ✅ Resilience Checks
- [ ] **Circuit breakers** implemented for external service calls
- [ ] **Fallback mechanisms** in place for critical services
- [ ] **Timeout handling** with appropriate cancellation tokens
- [ ] **Retry logic** with exponential backoff where appropriate
- [ ] **Graceful degradation** when services are unavailable

### ✅ Circuit Breaker Implementation
```csharp
// ✅ GOOD - Circuit Breaker Pattern
public async Task<string> SaveFileAsync(FileUploadRequest request)
{
    return await _circuitBreaker.ExecuteAsync(
        async () => await _storageService.SaveFileAsync(request),
        "storage_save_file"
    );
}
```

### ✅ Fallback Mechanisms
```csharp
// ✅ GOOD - Service Fallback
public async Task<IStorageService> GetStorageServiceAsync()
{
    if (_s3Service.IsAvailable())
    {
        return _s3Service;
    }
    
    _logger.LogWarning("S3 service unavailable, falling back to in-memory storage");
    return _inMemoryService;
}
```

### ✅ Health Check Integration
```csharp
// ✅ GOOD - Health Check
public async Task<HealthCheckResult> CheckHealthAsync()
{
    try
    {
        var isHealthy = await _service.IsHealthyAsync();
        return isHealthy 
            ? HealthCheckResult.Healthy("Service operational")
            : HealthCheckResult.Degraded("Service experiencing issues");
    }
    catch (Exception ex)
    {
        return HealthCheckResult.Unhealthy("Service health check failed", ex);
    }
}
```

## Metrics & Monitoring

### ✅ Metrics Collection
- [ ] **Performance metrics** recorded for key operations
- [ ] **Business metrics** tracked where applicable
- [ ] **Error rates** monitored and alerted on
- [ ] **Resource usage** metrics collected

### ✅ Metrics Implementation
```csharp
// ✅ GOOD - Metrics Collection
public async Task<string> ProcessRequestAsync(string data)
{
    using var timer = _metricsService.StartTimer("request_processing");
    
    try
    {
        var result = await _service.ProcessAsync(data);
        _metricsService.IncrementCounter("requests_successful");
        return result;
    }
    catch (Exception ex)
    {
        _metricsService.IncrementCounter("requests_failed", new Dictionary<string, string>
        {
            ["error_type"] = ex.GetType().Name
        });
        throw;
    }
}
```

## Testing Standards

### ✅ Test Coverage
- [ ] **Unit tests** for all business logic
- [ ] **Integration tests** for service interactions
- [ ] **Chaos tests** for failure scenarios
- [ ] **Performance tests** for critical paths

### ✅ Chaos Test Examples
```csharp
// ✅ GOOD - Chaos Test
[TestMethod]
public async Task System_ShouldDegradeGracefully_WhenDatabaseFails()
{
    // Arrange
    await SimulateDatabaseFailureAsync();
    
    // Act
    var response = await _client.GetAsync("/api/data");
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    var healthStatus = await GetHealthStatusAsync();
    Assert.AreEqual("degraded", healthStatus.Database.Status);
}
```

## Configuration Standards

### ✅ Configuration Validation
- [ ] **Configuration validation** implemented
- [ ] **Environment-specific** settings properly configured
- [ ] **Sensitive configuration** stored securely
- [ ] **Default values** provided for optional settings

### ✅ Configuration Example
```csharp
// ✅ GOOD - Configuration Validation
public class ServiceConfigurationOptions
{
    [Required(ErrorMessage = "Database connection string is required")]
    public string DatabaseConnectionString { get; set; } = string.Empty;
    
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;
    
    [Required(ErrorMessage = "Retry configuration is required")]
    public RetryConfiguration Retry { get; set; } = new();
}
```

## Security Standards

### ✅ Security Checks
- [ ] **No secrets** in code or logs
- [ ] **Input validation** implemented
- [ ] **Authentication/Authorization** properly configured
- [ ] **HTTPS** enforced in production
- [ ] **CORS** properly configured

## Performance Standards

### ✅ Performance Checks
- [ ] **Async/await** used consistently
- [ ] **No blocking operations** in async methods
- [ ] **Resource disposal** implemented (using statements)
- [ ] **Memory leaks** avoided
- [ ] **Database queries** optimized

## Documentation Standards

### ✅ Documentation Requirements
- [ ] **Code comments** for complex logic
- [ ] **API documentation** updated
- [ ] **README** updated if needed
- [ ] **Architecture decisions** documented

## Review Process

### Before Approving
- [ ] All checklist items reviewed
- [ ] Tests pass locally
- [ ] No security vulnerabilities introduced
- [ ] Performance impact assessed
- [ ] Documentation updated

### Review Comments
When commenting on reviews, use these prefixes:
- `[LOGGING]` - Logging-related issues
- `[CHAOS]` - Chaos engineering/resilience issues
- `[SECURITY]` - Security-related issues
- `[PERFORMANCE]` - Performance-related issues
- `[TESTING]` - Testing-related issues

### Example Review Comments
```
[LOGGING] Missing correlation ID in this log entry
[CHAOS] This external call needs a circuit breaker
[SECURITY] This input should be validated
[PERFORMANCE] Consider caching this result
[TESTING] Add a chaos test for this failure scenario
```

## Automated Checks

### CI/CD Pipeline
- [ ] **SonarQube** analysis passes
- [ ] **Code coverage** meets minimum threshold
- [ ] **Security scan** passes
- [ ] **Performance tests** pass
- [ ] **Chaos tests** run successfully

### Pre-commit Hooks
- [ ] **Code formatting** applied
- [ ] **Linting** passes
- [ ] **Unit tests** run
- [ ] **Security checks** performed

## Post-Review Actions

### After Approval
- [ ] **Merge** to main branch
- [ ] **Deploy** to staging environment
- [ ] **Run chaos tests** in staging
- [ ] **Monitor** system behavior
- [ ] **Update** documentation if needed

### Monitoring
- [ ] **Watch logs** for any issues
- [ ] **Monitor metrics** for performance impact
- [ ] **Check alerts** for any problems
- [ ] **Validate** chaos engineering resilience

## Resources

### Documentation
- [Chaos Engineering Guide](../docs/CHAOS_ENGINEERING_LOGGING_ARCHITECTURE.md)
- [Logging Standards](../docs/LOGGING_STANDARDS.md)
- [Testing Guidelines](../docs/TESTING_GUIDELINES.md)

### Tools
- [Chaos Test Runner](../scripts/chaos-test-runner.ps1)
- [Code Quality Checks](../scripts/code-quality.ps1)
- [Performance Monitoring](../scripts/performance-monitor.ps1)

### Templates
- [Chaos Test Template](../templates/chaos-test-template.cs)
- [Circuit Breaker Template](../templates/circuit-breaker-template.cs)
- [Health Check Template](../templates/health-check-template.cs) 