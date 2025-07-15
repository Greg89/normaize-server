# Auth0 Integration Guide

## Overview

The Normaize application integrates with Auth0 for JWT-based authentication. This document describes the integration points, expected claim types, and middleware behavior.

## Authentication Flow

1. **Client Authentication**: Users authenticate through Auth0
2. **JWT Token**: Auth0 issues a JWT token containing user claims
3. **API Requests**: Client includes JWT token in Authorization header
4. **Middleware Processing**: Auth0Middleware extracts user information from claims
5. **Controller Access**: Controllers access user information via HttpContext.Items

## Expected Auth0 Claim Types

### Required Claims

| Claim Type | Standard Claim | Auth0 Claim | Description | Example |
|------------|----------------|--------------|-------------|---------|
| User ID | `nameidentifier` | `sub` | Unique user identifier | `auth0\|123456789` |
| Email | `email` | `email` | User's email address | `user@example.com` |
| Name | `name` | `name` | User's display name | `John Doe` |

### Optional Claims

| Claim Type | Standard Claim | Auth0 Claim | Description |
|------------|----------------|--------------|-------------|
| Picture | `picture` | `picture` | User's profile picture URL |
| Email Verified | `email_verified` | `email_verified` | Whether email is verified |
| Updated At | `updated_at` | `updated_at` | Last profile update timestamp |

## Auth0Middleware Behavior

### Authenticated Requests

When a user is authenticated, the middleware:

1. **Extracts Claims**: Reads user ID, email, and name from JWT claims
2. **Fallback Logic**: Uses `sub` claim if `nameidentifier` is not present
3. **Context Storage**: Stores user information in `HttpContext.Items`
4. **Logging**: Logs debug information about the authenticated user

### Unauthenticated Requests

When a user is not authenticated, the middleware:

1. **Skips Processing**: Does not extract or store user information
2. **Logging**: Logs debug information about the unauthenticated request
3. **Continues Pipeline**: Calls the next middleware in the pipeline

### Context Items

The middleware adds the following items to `HttpContext.Items`:

```csharp
context.Items["UserId"] = userId;      // string
context.Items["UserEmail"] = email;    // string
context.Items["UserName"] = name;      // string
```

## Usage in Controllers

### Accessing User Information

Controllers can access user information in two ways:

#### Method 1: HttpContext.Items (Recommended)

```csharp
public class DataSetsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDataSets()
    {
        var userId = HttpContext.Items["UserId"] as string;
        var userEmail = HttpContext.Items["UserEmail"] as string;
        
        // Use user information...
    }
}
```

#### Method 2: Claims Principal

```csharp
public class DataSetsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDataSets()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        
        // Use user information...
    }
}
```

## Configuration

### Auth0 Settings

Configure the following Auth0 settings in your application:

```json
{
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "Audience": "https://your-api.com",
    "ClientId": "your-client-id"
  }
}
```

### JWT Bearer Configuration

The application is configured to validate JWT tokens from Auth0:

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{Configuration["Auth0:Domain"]}/";
        options.Audience = Configuration["Auth0:Audience"];
    });
```

## Testing

### Unit Tests

The Auth0Middleware includes comprehensive unit tests covering:

- Authenticated user claim extraction
- Auth0 `sub` claim fallback
- Missing claims handling
- Unauthenticated request processing
- Logging behavior
- Middleware pipeline continuation

### Integration Tests

For integration testing with Auth0:

1. **Test Tokens**: Use Auth0's test token generation
2. **Mock Authentication**: Mock the authentication middleware
3. **Claim Validation**: Verify expected claims are present

## Troubleshooting

### Common Issues

#### Missing User ID

**Symptoms**: `UserId` is null in controllers
**Causes**: 
- JWT token missing `sub` or `nameidentifier` claim
- Auth0 configuration issues
- Token validation failures

**Solutions**:
1. Verify Auth0 application settings
2. Check JWT token claims using jwt.io
3. Enable debug logging for middleware

#### Authentication Failures

**Symptoms**: 401 Unauthorized responses
**Causes**:
- Invalid JWT token
- Expired token
- Incorrect audience or issuer
- Missing Authorization header

**Solutions**:
1. Verify token expiration
2. Check Auth0 domain and audience configuration
3. Ensure proper Authorization header format: `Bearer <token>`

### Debug Logging

Enable debug logging to troubleshoot authentication issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Auth0Middleware": "Debug"
    }
  }
}
```

## Security Considerations

1. **Token Validation**: Always validate JWT tokens on the server
2. **HTTPS Only**: Use HTTPS in production for all API calls
3. **Token Expiration**: Implement proper token refresh logic
4. **Scope Validation**: Validate required scopes for protected endpoints
5. **Rate Limiting**: Implement rate limiting for authentication endpoints

## Best Practices

1. **Use HttpContext.Items**: Prefer `HttpContext.Items` over direct claim access
2. **Handle Null Values**: Always check for null user information
3. **Logging**: Use structured logging for authentication events
4. **Error Handling**: Implement proper error handling for authentication failures
5. **Testing**: Write comprehensive tests for authentication scenarios 