# Auth0 Extension Methods Usage Guide

## Overview
This document explains how to use the Auth0 extension methods for working with JWT tokens across the application.

## Extension Methods

### `GetUserId()`
Gets the user ID from the JWT token claims.

```csharp
// Basic usage
var userId = User.GetUserId();

// This will throw UnauthorizedAccessException if no user ID is found
```

### `GetUserIdWithFallback(string? fallbackUserId = null)`
Gets the user ID with fallback support for client credentials tokens.

```csharp
// With default fallback
var userId = User.GetUserIdWithFallback();

// With custom fallback
var userId = User.GetUserIdWithFallback("custom-user-id");
```

### `IsClientCredentialsToken()`
Checks if the current token is a client credentials token.

```csharp
var isClientCredentials = User.IsClientCredentialsToken();
```

### `GetGrantType()`
Gets the grant type from the JWT token.

```csharp
var grantType = User.GetGrantType(); // Returns "client-credentials", "password", etc.
```

## Usage Examples

### In Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExampleController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> GetUserInfo()
    {
        // Get user ID with fallback for client credentials
        var userId = User.GetUserIdWithFallback("test-user");
        
        // Check token type
        var isClientCredentials = User.IsClientCredentialsToken();
        var grantType = User.GetGrantType();
        
        return Ok(new
        {
            userId = userId,
            isClientCredentials = isClientCredentials,
            grantType = grantType
        });
    }
    
    [HttpGet("strict")]
    public ActionResult<object> GetStrictUserInfo()
    {
        // This will throw if no user ID is found (no fallback)
        var userId = User.GetUserId();
        
        return Ok(new { userId = userId });
    }
}
```

### In Services

```csharp
public class ExampleService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ExampleService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            throw new UnauthorizedAccessException("User context not available");
            
        return user.GetUserIdWithFallback();
    }
}
```

## Benefits

### 1. **Consistency**
- All controllers use the same logic for extracting user IDs
- Consistent error handling across the application
- Standardized approach to Auth0 token processing

### 2. **Maintainability**
- Single place to update Auth0 claim extraction logic
- Easy to modify fallback behavior
- Centralized token type detection

### 3. **Flexibility**
- Support for both user tokens and client credentials tokens
- Configurable fallback user IDs
- Easy to extend with additional token analysis

### 4. **Error Handling**
- Consistent exception types
- Clear error messages
- Graceful fallback for development/testing

## Best Practices

### 1. **Use GetUserIdWithFallback() for Development**
```csharp
// Good for development/testing
var userId = User.GetUserIdWithFallback("test-user");
```

### 2. **Use GetUserId() for Production**
```csharp
// Good for production - strict user validation
var userId = User.GetUserId();
```

### 3. **Check Token Type When Needed**
```csharp
if (User.IsClientCredentialsToken())
{
    // Handle client credentials specific logic
}
else
{
    // Handle user token logic
}
```

### 4. **Log Token Information**
```csharp
_logger.LogInformation("Token type: {GrantType}, Client credentials: {IsClientCredentials}", 
    User.GetGrantType(), 
    User.IsClientCredentialsToken());
```

## Migration Guide

### From Manual Claim Extraction
**Before:**
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found");
```

**After:**
```csharp
var userId = User.GetUserId();
```

### From Client Credentials Detection
**Before:**
```csharp
var isClientCredentials = userId.EndsWith("@clients");
```

**After:**
```csharp
var isClientCredentials = User.IsClientCredentialsToken();
```

## Testing

### Unit Tests
```csharp
[Test]
public void GetUserId_WithValidToken_ReturnsUserId()
{
    // Arrange
    var claims = new List<Claim>
    {
        new Claim("sub", "user123")
    };
    var identity = new ClaimsIdentity(claims);
    var principal = new ClaimsPrincipal(identity);
    
    // Act
    var userId = principal.GetUserId();
    
    // Assert
    Assert.AreEqual("user123", userId);
}
```

### Integration Tests
```csharp
[Test]
public async Task Controller_WithClientCredentials_ReturnsFallbackUserId()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", clientCredentialsToken);
    
    // Act
    var response = await client.GetAsync("/api/example");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<dynamic>();
    Assert.AreEqual("test-user", result.userId);
}
```

## Security Considerations

1. **Client Credentials Tokens**: These are machine-to-machine tokens and should be handled differently than user tokens
2. **Fallback User IDs**: Only use fallback user IDs in development/testing environments
3. **Token Validation**: Always validate tokens before extracting claims
4. **Error Handling**: Don't expose sensitive token information in error messages

## Future Enhancements

1. **Role-Based Extensions**: Add methods for extracting roles and permissions
2. **Token Expiration**: Add methods for checking token expiration
3. **Audience Validation**: Add methods for validating token audience
4. **Custom Claims**: Add methods for extracting custom Auth0 claims 