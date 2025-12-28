# Chaos Engineering & Logging Architecture

## Quick Start

This project implements a comprehensive chaos engineering and logging architecture designed for high resilience and observability.

### 🚀 Getting Started

1. **Review the Architecture**: [Chaos Engineering Guide](docs/CHAOS_ENGINEERING_LOGGING_ARCHITECTURE.md)
2. **Run Chaos Tests**: `./scripts/chaos-test-runner.ps1 -Environment Development -TestType Infrastructure`
3. **Check Logging Standards**: [Code Review Checklist](scripts/code-review-checklist.md)
4. **Use Templates**: [Chaos Test Template](templates/chaos-test-template.cs)

### 📋 Key Features

- ✅ **SonarQube Compliant**: No log-and-rethrow patterns
- ✅ **Correlation Tracking**: Full request tracing
- ✅ **Circuit Breakers**: External dependency resilience
- ✅ **Graceful Degradation**: Multiple fallback mechanisms
- ✅ **Automated Testing**: Chaos test framework
- ✅ **Comprehensive Monitoring**: Metrics and health checks

### 🔧 Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LIFECYCLE                     │
├─────────────────────────────────────────────────────────────┤
│ 1. PROGRAM.CS (Application Level)                           │
│    ├─ Logs application start/stop                          │
│    └─ Global exception handling                            │
│                                                             │
│ 2. SERVICECONFIGURATION.CS (Service Level)                 │
│    ├─ Single log point for service config failures         │
│    └─ Exceptions bubble up to Program.cs                   │
│                                                             │
│ 3. MIDDLEWARECONFIGURATION.CS (Middleware Level)           │
│    ├─ Single log point for middleware config failures      │
│    └─ Exceptions bubble up to Program.cs                   │
│                                                             │
│ 4. RUNTIME EXCEPTION HANDLING                              │
│    ├─ ExceptionHandlingMiddleware (Global)                 │
│    ├─ RequestLoggingMiddleware (Request-specific)          │
│    └─ Service Layer (Business logic)                       │
└─────────────────────────────────────────────────────────────┘
```

### 🧪 Running Chaos Tests

#### Prerequisites
- .NET 9.0 SDK
- PowerShell 7.0+
- API running on localhost:5000 (or specify with `-ApiUrl`)

#### Basic Usage
```powershell
# Run infrastructure chaos tests
./scripts/chaos-test-runner.ps1 -Environment Development -TestType Infrastructure

# Run dependency chaos tests
./scripts/chaos-test-runner.ps1 -Environment Beta -TestType Dependency -Duration 10

# Run application chaos tests
./scripts/chaos-test-runner.ps1 -Environment Production -TestType Application -ApiUrl "https://api.normaize.com"
```

#### Test Types
- **Infrastructure**: Database, storage, network failures
- **Application**: Memory, CPU, thread pool issues
- **Dependency**: External API, auth service failures

### 📊 Monitoring & Metrics

#### Health Checks
- **Readiness**: `/health/readiness`
- **Liveness**: `/health/liveness`
- **Detailed**: `/health/detailed`

#### Metrics Endpoints
- **Performance**: `/api/metrics/performance`
- **Circuit Breakers**: `/api/metrics/circuit-breakers`
- **Exceptions**: `/api/metrics/exceptions`

#### Logging
- **Structured**: Serilog with correlation IDs
- **Levels**: Debug, Info, Warning, Error, Critical
- **Context**: User, request, environment information

### 🔍 Code Review Standards

#### Logging Checklist
- [ ] No log-and-rethrow patterns
- [ ] Correlation IDs included
- [ ] Structured logging used
- [ ] Appropriate log levels
- [ ] No sensitive data logged

#### Chaos Engineering Checklist
- [ ] Circuit breakers for external calls
- [ ] Fallback mechanisms in place
- [ ] Health checks comprehensive
- [ ] Metrics collected
- [ ] Chaos tests cover scenarios

### 🛠️ Development Workflow

#### 1. New Feature Development
```bash
# 1. Create feature branch
git checkout -b feature/new-resilient-service

# 2. Implement with logging standards
# - Use correlation IDs
# - Implement circuit breakers
# - Add health checks
# - Write chaos tests

# 3. Run tests
dotnet test
./scripts/chaos-test-runner.ps1 -Environment Development -TestType Infrastructure

# 4. Code review using checklist
# - Review logging standards
# - Review chaos engineering standards
# - Run chaos tests

# 5. Merge and deploy
git merge main
# Deploy to staging
# Run chaos tests in staging
# Monitor system behavior
```

#### 2. Adding New Chaos Tests
```csharp
// 1. Create new test class
public class NewFailureScenarioTest : ChaosTestTemplate
{
    protected override string TestName => "New Failure Scenario";
    protected override TimeSpan TestDuration => TimeSpan.FromMinutes(5);
    
    protected override async Task ExecuteChaosScenarioAsync(string correlationId, CancellationToken cancellationToken)
    {
        // Implement failure scenario
    }
}

// 2. Add to test suite
// 3. Update documentation
// 4. Run validation tests
```

#### 3. Monitoring New Services
```csharp
// 1. Add health check
public class NewServiceHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Implement health check logic
    }
}

// 2. Add metrics collection
// 3. Configure alerts
// 4. Update monitoring dashboard
```

### 📈 Performance Monitoring

#### Key Metrics
- **Request Duration**: Response time tracking
- **Error Rates**: Exception frequency
- **Circuit Breaker States**: External dependency health
- **Resource Usage**: CPU, memory, disk
- **Business Metrics**: User actions, data processing

#### Alerting
- **High Error Rate**: > 5% error rate for 5 minutes
- **Circuit Breaker Open**: Any circuit breaker open for > 5 minutes
- **High Response Time**: > 2 seconds average response time
- **Chaos Test Failure**: Any chaos test fails

### 🔧 Configuration

#### Chaos Engineering Settings
```json
{
  "ChaosEngineering": {
    "EnableChaosTests": true,
    "TestInterval": "01:00:00",
    "MaxConcurrentTests": 2,
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "ResetTimeout": "00:01:00",
      "MonitoringWindow": "00:05:00"
    },
    "Metrics": {
      "EnablePrometheus": true,
      "PrometheusEndpoint": "/metrics"
    }
  }
}
```

#### Logging Configuration
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

### 🚨 Incident Response

#### When Chaos Tests Fail
1. **Immediate Actions**
   - Check system health status
   - Review recent logs for errors
   - Verify fallback mechanisms
   - Check circuit breaker states

2. **Investigation**
   - Analyze monitoring data
   - Review chaos test results
   - Check for configuration changes
   - Validate external dependencies

3. **Recovery**
   - Restore failed services
   - Verify system recovery
   - Update documentation
   - Plan improvements

#### Communication
- **Slack**: #chaos-engineering-alerts
- **Email**: chaos-engineering@normaize.com
- **Dashboard**: https://monitoring.normaize.com/chaos

### 📚 Resources

#### Documentation
- [Comprehensive Guide](docs/CHAOS_ENGINEERING_LOGGING_ARCHITECTURE.md)
- [Code Review Checklist](scripts/code-review-checklist.md)
- [Testing Guidelines](docs/TESTING_GUIDELINES.md)

#### Templates
- [Chaos Test Template](templates/chaos-test-template.cs)
- [Circuit Breaker Template](templates/circuit-breaker-template.cs)
- [Health Check Template](templates/health-check-template.cs)

#### Scripts
- [Chaos Test Runner](scripts/chaos-test-runner.ps1)
- [Code Quality Checks](scripts/code-quality.ps1)
- [Performance Monitor](scripts/performance-monitor.ps1)

### 🤝 Contributing

#### Guidelines
1. **Follow Logging Standards**: Use correlation IDs and structured logging
2. **Implement Resilience**: Add circuit breakers and fallbacks
3. **Write Chaos Tests**: Cover failure scenarios
4. **Update Documentation**: Keep guides current
5. **Monitor Performance**: Track metrics and alerts

#### Review Process
1. **Self-Review**: Use the code review checklist
2. **Peer Review**: Have another developer review
3. **Chaos Testing**: Run chaos tests locally
4. **Staging Validation**: Test in staging environment
5. **Production Monitoring**: Watch metrics after deployment

### 📞 Support

#### Team Contacts
- **Chaos Engineering Lead**: chaos-lead@normaize.com
- **DevOps Team**: devops@normaize.com
- **Architecture Team**: architecture@normaize.com

#### Emergency Contacts
- **On-Call Engineer**: +1-555-CHOAS-01
- **System Administrator**: +1-555-SYSADM-01

---

**Remember**: Chaos engineering is not about breaking things, but about building confidence in your system's ability to handle failures gracefully. Always test in a controlled environment first! 