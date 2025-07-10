# Health Check System

This document describes the comprehensive health check system implemented in the Normaize application, following industry standards for production-ready applications.

## Overview

The health check system provides three types of health checks following Kubernetes and industry best practices:

1. **Liveness Probe** - Is the application alive?
2. **Readiness Probe** - Is the application ready to serve traffic?
3. **Comprehensive Health Check** - Detailed health of all components

## Health Check Endpoints

### 1. Liveness Check (`/health/liveness`)
**Purpose**: Verify the application is responsive and not in a deadlock state.

**Response Time**: < 100ms
**Use Case**: Kubernetes liveness probe, basic application status

```json
{
  "status": "alive",
  "timestamp": "2024-01-15T10:30:00Z",
  "duration": 45.2,
  "message": "Application is alive"
}
```

**Checks**:
- Application responsiveness
- Basic database connectivity
- No pending migrations

### 2. Readiness Check (`/health/readiness`)
**Purpose**: Verify the application is ready to serve traffic.

**Response Time**: < 500ms
**Use Case**: Kubernetes readiness probe, load balancer health checks

```json
{
  "status": "ready",
  "components": {
    "database": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 120.5,
      "details": {
        "missingColumns": [],
        "connectionString": "configured"
      }
    },
    "application": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 45.2,
      "details": {
        "pendingMigrations": [],
        "canConnect": true,
        "environment": "Production"
      }
    },
    "storage": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 25.1,
      "details": {
        "provider": "local",
        "type": "in_memory"
      }
    }
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "duration": 190.8,
  "message": "Application is ready to serve traffic"
}
```

**Checks**:
- Database connectivity and schema
- Application health
- Storage service availability
- No critical issues

### 3. Comprehensive Health Check (`/health/health`)
**Purpose**: Detailed health information for monitoring and debugging.

**Response Time**: < 2 seconds
**Use Case**: Monitoring dashboards, detailed troubleshooting

```json
{
  "status": "healthy",
  "components": {
    "database": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 120.5,
      "details": {
        "missingColumns": [],
        "connectionString": "configured"
      }
    },
    "application": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 45.2,
      "details": {
        "pendingMigrations": [],
        "canConnect": true,
        "environment": "Production"
      }
    },
    "storage": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 25.1,
      "details": {
        "provider": "local",
        "type": "in_memory"
      }
    },
    "external_services": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 15.3,
      "details": {
        "seq_logging": "configured"
      }
    },
    "system_resources": {
      "isHealthy": true,
      "status": "healthy",
      "duration": 8.7,
      "details": {
        "memory_usage_mb": 45.2,
        "memory_usage_percent": 12.3,
        "max_memory_mb": 367.5,
        "disk_free_gb": 15.7,
        "disk_free_percent": 85.2,
        "available_worker_threads": 1023,
        "available_completion_port_threads": 1000
      }
    }
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "duration": 214.8,
  "message": "All systems healthy"
}
```

**Checks**:
- All readiness checks
- External service configuration
- System resource monitoring
- Detailed performance metrics

## Component Health Checks

### Database Health
- **Connectivity**: Can connect to MySQL database
- **Schema**: All required columns and tables exist
- **Migrations**: No pending migrations
- **Performance**: Connection response time

### Application Health
- **Responsiveness**: Application is responding
- **Configuration**: Environment variables are set
- **Dependencies**: Core services are available
- **Environment**: Correct environment configuration

### Storage Health
- **Provider**: Storage service is configured
- **Connectivity**: Can access storage (if external)
- **Type**: In-memory, local, or external storage

### External Services Health
- **Auth0**: Authentication configuration
- **Seq**: Logging service configuration
- **Other Services**: Any additional external dependencies

### System Resources Health
- **Memory**: Memory usage and availability
- **Disk Space**: Available disk space
- **Thread Pool**: Available worker threads
- **Performance**: Response times and throughput

## Health Check Categories

### Liveness Probe
**When to use**: Kubernetes liveness probe, basic status checks
**Frequency**: Every 30 seconds
**Timeout**: 10 seconds
**Failure Action**: Restart container

```yaml
# Kubernetes example
livenessProbe:
  httpGet:
    path: /health/liveness
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 30
  timeoutSeconds: 10
  failureThreshold: 3
```

### Readiness Probe
**When to use**: Kubernetes readiness probe, load balancer health checks
**Frequency**: Every 30 seconds
**Timeout**: 10 seconds
**Failure Action**: Remove from load balancer

```yaml
# Kubernetes example
readinessProbe:
  httpGet:
    path: /health/readiness
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 30
  timeoutSeconds: 10
  failureThreshold: 3
```

### Comprehensive Health Check
**When to use**: Monitoring dashboards, detailed troubleshooting
**Frequency**: Every 5 minutes
**Timeout**: 30 seconds
**Failure Action**: Alert operators

## Configuration

### Environment Variables
```bash
# Health check configuration
HEALTH_CHECK_TIMEOUT=30000  # 30 seconds
HEALTH_CHECK_INTERVAL=300000  # 5 minutes
```

### Railway Configuration
```json
{
  "deploy": {
    "healthcheckPath": "/health/readiness",
    "healthcheckTimeout": 300,
    "restartPolicyType": "on_failure",
    "healthcheckInterval": 30,
    "healthcheckRetries": 3
  }
}
```

### Docker Configuration
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health/readiness || exit 1
```

## Monitoring and Alerting

### Health Check Monitoring
```bash
# Check liveness
curl -f http://localhost:8080/health/liveness

# Check readiness
curl -f http://localhost:8080/health/readiness

# Comprehensive health check
curl http://localhost:8080/health/database
```

### Alerting Rules
```yaml
# Prometheus alerting rules example
groups:
  - name: health_checks
    rules:
      - alert: ApplicationNotReady
        expr: up{job="normaize-api"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Application is not ready"
          description: "Health check failed for {{ $labels.instance }}"

      - alert: DatabaseUnhealthy
        expr: normaize_database_health == 0
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "Database is unhealthy"
          description: "Database health check failed for {{ $labels.instance }}"
```

### Logging
Health check results are logged with structured logging:

```json
{
  "level": "Information",
  "message": "Comprehensive health check completed successfully in 214.8ms",
  "properties": {
    "duration": 214.8,
    "components": ["database", "application", "storage", "external_services", "system_resources"],
    "status": "healthy"
  }
}
```

## Troubleshooting

### Common Issues

#### 1. Health Check Timeouts
**Symptoms**: Health checks fail with timeout errors
**Solutions**:
- Check database connectivity
- Verify external service availability
- Review system resource usage

#### 2. Database Health Failures
**Symptoms**: Database component shows unhealthy
**Solutions**:
- Check database connection string
- Verify schema integrity
- Run migration fixes

#### 3. System Resource Issues
**Symptoms**: High memory or disk usage
**Solutions**:
- Monitor resource usage
- Scale application resources
- Clean up temporary files

### Debugging Commands
```bash
# Check specific component health
curl http://localhost:8080/health/database | jq '.components.database'

# Check system resources
curl http://localhost:8080/health/database | jq '.components.system_resources'

# Monitor health check performance
curl -w "@curl-format.txt" http://localhost:8080/health/readiness
```

## Best Practices

### 1. Use Appropriate Health Checks
- **Liveness**: For basic application status
- **Readiness**: For load balancer and traffic routing
- **Comprehensive**: For monitoring and debugging

### 2. Set Proper Timeouts
- **Liveness**: 10 seconds (quick check)
- **Readiness**: 10 seconds (moderate check)
- **Comprehensive**: 30 seconds (detailed check)

### 3. Monitor Health Check Performance
- Track response times
- Alert on slow health checks
- Optimize health check logic

### 4. Use Health Checks for Load Balancing
- Configure load balancers to use readiness checks
- Remove unhealthy instances from traffic
- Ensure graceful degradation

### 5. Implement Circuit Breakers
- Use health check results for circuit breaker decisions
- Fail fast when dependencies are unhealthy
- Provide fallback mechanisms

## Summary

The health check system provides:

1. **Industry Standard Compliance**: Follows Kubernetes and cloud-native best practices
2. **Comprehensive Monitoring**: Covers all application components
3. **Performance Tracking**: Monitors response times and resource usage
4. **Troubleshooting Support**: Detailed health information for debugging
5. **Production Ready**: Fail-fast behavior and proper error handling

This ensures your application is properly monitored and can be quickly diagnosed when issues occur. 