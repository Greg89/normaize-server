# Structured Logging with Serilog and Seq

This document describes the structured logging implementation for the Normaize API using Serilog and Seq.

## Overview

The application uses **Serilog** for structured logging with the following features:

- **Structured Logging**: All logs include structured data for better querying and analysis
- **User Context**: Every log entry includes user information when available
- **Request Tracing**: Complete request/response cycle logging with timing
- **Exception Handling**: Comprehensive exception logging with full context
- **Environment Support**: Different logging configurations for Development vs Production
- **Seq Integration**: Centralized log aggregation for production environments

## Architecture

### Components

1. **StructuredLoggingService**: Core logging service that provides user context and structured logging methods
2. **RequestLoggingMiddleware**: Middleware that logs all HTTP requests with timing and user context
3. **ExceptionHandlingMiddleware**: Global exception handler with structured logging
4. **Serilog Configuration**: Environment-specific logging setup in Program.cs

### Log Structure

Every log entry includes:

- **Timestamp**: When the event occurred
- **Log Level**: Information, Warning, Error, etc.
- **User Context**: User ID and email (when authenticated)
- **Environment**: Development, Beta, Production
- **Request Context**: HTTP method, path, status code, duration
- **Structured Data**: Additional context-specific data

## Configuration

### Environment Variables

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `SEQ_URL` | Seq server URL | No* | - |
| `SEQ_API_KEY` | Seq API key for authentication | No* | - |
| `ASPNETCORE_ENVIRONMENT` | Application environment | No | Development |

*Seq logging is only enabled in non-Development environments when `SEQ_URL` is provided.

### Local Development

In local development, logs are written to the console with structured formatting:

```json
[12:34:56 INF] User Action: GetDataSets by User: user123 (user@example.com)
[12:34:57 INF] Request Completed: GET /api/datasets - Status: 200 - Duration: 150ms - User: user123
```

### Production (Railway)

In production environments with Seq configured:

1. **Console Logging**: Still active for immediate debugging
2. **Seq Logging**: All logs sent to Seq for centralized aggregation
3. **Structured Data**: Full structured logging with user context and request tracing

## Usage

### Basic Logging

```csharp
// Inject the logging service
private readonly IStructuredLoggingService _loggingService;

// Log user actions
_loggingService.LogUserAction("Dataset uploaded", new { datasetId = 123, fileName = "data.csv" });

// Log exceptions
try
{
    // Some operation
}
catch (Exception ex)
{
    _loggingService.LogException(ex, "Data processing failed");
}
```

### Request Logging

Request logging is automatic and includes:

- **Request Start**: Method, path, user ID
- **Request End**: Status code, duration, user ID
- **Exceptions**: Full exception context with user and request info

### User Context

User context is automatically extracted from:

1. **Auth0 JWT Token**: User ID and email from claims
2. **HTTP Context**: Fallback to context items if needed

## Log Examples

### User Action Log
```json
{
  "Timestamp": "2024-01-15T10:30:45.123Z",
  "Level": "Information",
  "Message": "User Action: Dataset uploaded by User: user123 (user@example.com)",
  "Properties": {
    "UserId": "user123",
    "UserEmail": "user@example.com",
    "Action": "Dataset uploaded",
    "Environment": "Production"
  }
}
```

### Exception Log
```json
{
  "Timestamp": "2024-01-15T10:30:46.456Z",
  "Level": "Error",
  "Message": "Exception in UploadDataSet - User: user123 (user@example.com) - Request: POST /api/datasets/upload",
  "Exception": "System.IO.IOException: File not found",
  "Properties": {
    "UserId": "user123",
    "UserEmail": "user@example.com",
    "Context": "UploadDataSet",
    "RequestMethod": "POST",
    "RequestPath": "/api/datasets/upload",
    "Environment": "Production"
  }
}
```

### Request Log
```json
{
  "Timestamp": "2024-01-15T10:30:47.789Z",
  "Level": "Information",
  "Message": "Request Completed: GET /api/datasets - Status: 200 - Duration: 150ms - User: user123",
  "Properties": {
    "UserId": "user123",
    "Method": "GET",
    "Path": "/api/datasets",
    "StatusCode": 200,
    "DurationMs": 150,
    "Environment": "Production"
  }
}
```

## Seq Queries

With Seq, you can perform powerful queries:

### Find all actions by a specific user
```
UserId = "user123"
```

### Find all exceptions in the last hour
```
@Level = "Error" and @Timestamp > now() - 1h
```

### Find slow requests (>1 second)
```
DurationMs > 1000
```

### Find all requests for a specific endpoint
```
Path = "/api/datasets/upload"
```

### Find user actions with specific data
```
Action = "Dataset uploaded" and ActionData.datasetId = 123
```

## Best Practices

### 1. Use Structured Logging
```csharp
// Good: Structured data
_loggingService.LogUserAction("Dataset uploaded", new { datasetId = 123, fileName = "data.csv" });

// Avoid: String concatenation
_loggingService.LogUserAction($"Dataset {datasetId} uploaded with file {fileName}");
```

### 2. Provide Context
```csharp
// Good: Include context
_loggingService.LogException(ex, "Data processing failed");

// Avoid: Generic messages
_loggingService.LogException(ex, "Error occurred");
```

### 3. Use Appropriate Log Levels
- **Information**: Normal application flow, user actions
- **Warning**: Unexpected but handled situations
- **Error**: Exceptions and failures
- **Fatal**: Application startup/shutdown failures

### 4. Don't Log Sensitive Data
```csharp
// Good: Log action without sensitive data
_loggingService.LogUserAction("User logged in", new { userId = "user123" });

// Avoid: Logging passwords, tokens, etc.
_loggingService.LogUserAction("User logged in", new { password = "secret123" });
```

## Monitoring and Alerting

### Seq Alerts

Configure Seq alerts for:

- **High Error Rate**: Alert when error percentage exceeds threshold
- **Slow Requests**: Alert when average response time is high
- **User Issues**: Alert when specific users encounter errors
- **System Health**: Alert on application startup/shutdown

### Health Checks

The application includes health check endpoints:

- `/health` - Basic health status
- `/health/basic` - Detailed health with environment info

## Troubleshooting

### Logs Not Appearing in Seq

1. Check `SEQ_URL` environment variable
2. Verify `SEQ_API_KEY` if authentication is enabled
3. Ensure environment is not "Development"
4. Check network connectivity to Seq instance

### Missing User Context

1. Verify Auth0 configuration
2. Check JWT token claims
3. Ensure authentication middleware is configured correctly

### Performance Impact

1. Logging is asynchronous and low-overhead
2. Console logging is buffered for performance
3. Seq logging uses HTTP with retry logic
4. Monitor log volume and adjust levels if needed 