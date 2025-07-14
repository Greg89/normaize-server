# Constants Guidelines

## Overview

This document outlines the standards for using constants in the Normaize application to maintain code quality and consistency.

## Constants Strategy

### 1. **Local Constants** (Preferred for single-class usage)
```csharp
public class MyController : ControllerBase
{
    private const string SUCCESS_MESSAGE = "Operation completed successfully";
    private const int MAX_RETRY_ATTEMPTS = 3;
    
    // Use within the class
    return Ok(SUCCESS_MESSAGE);
}
```

### 2. **Global Constants** (For application-wide usage)
```csharp
// In Normaize.Core/Constants/AppConstants.cs
public static class AppConstants
{
    public static class ConfigStatus
    {
        public const string SET = "SET";
        public const string NOT_SET = "NOT SET";
    }
}

// Usage across the application
using Normaize.Core.Constants;
var status = AppConstants.ConfigStatus.SET;
```

## When to Use Each Approach

### **Local Constants** ✅
- Used only within a single class
- Specific to that class's functionality
- Not shared across multiple files
- Simple, straightforward values

**Examples:**
- HTTP status codes
- Error messages
- Magic numbers
- Configuration keys

### **Global Constants** ✅
- Used across multiple classes/files
- Application-wide standards
- Shared business logic values
- Consistent messaging

**Examples:**
- Environment names
- Storage provider types
- Status indicators
- Common error messages

## Organization Structure

```
Normaize.Core/Constants/
├── AppConstants.cs          # Main constants file
├── HttpConstants.cs         # HTTP-related constants
├── ValidationConstants.cs   # Validation rules
└── BusinessConstants.cs     # Business logic constants
```

## Naming Conventions

### **Local Constants**
```csharp
private const string SUCCESS_MESSAGE = "Success";
private const int MAX_RETRY_COUNT = 3;
private const double DEFAULT_TIMEOUT = 30.0;
```

### **Global Constants**
```csharp
public static class AppConstants
{
    public static class ConfigStatus
    {
        public const string SET = "SET";
        public const string NOT_SET = "NOT SET";
    }
    
    public static class Environment
    {
        public const string DEVELOPMENT = "Development";
        public const string PRODUCTION = "Production";
    }
}
```

## Benefits

### **Code Quality**
- ✅ **SonarQube Compliance**: Eliminates magic string warnings
- ✅ **Maintainability**: Single source of truth for values
- ✅ **Refactoring**: Easy to change values across the application
- ✅ **Consistency**: Ensures uniform values throughout the codebase

### **Performance**
- ✅ **Compile-time**: Constants are resolved at compile time
- ✅ **Memory**: No runtime allocation for constant values
- ✅ **Optimization**: Compiler can optimize constant usage

### **Developer Experience**
- ✅ **IntelliSense**: Auto-completion for constant values
- ✅ **Type Safety**: Compile-time checking for valid values
- ✅ **Documentation**: Self-documenting code with meaningful names

## Best Practices

### **1. Use Descriptive Names**
```csharp
// Good
public const string CONFIG_STATUS_SET = "SET";

// Better
public const string CONFIG_STATUS_SET = "SET";
```

### **2. Group Related Constants**
```csharp
public static class AppConstants
{
    public static class ConfigStatus
    {
        public const string SET = "SET";
        public const string NOT_SET = "NOT SET";
    }
    
    public static class StorageProvider
    {
        public const string MEMORY = "memory";
        public const string S3 = "s3";
    }
}
```

### **3. Use XML Documentation**
```csharp
/// <summary>
/// Configuration status constants for environment variables
/// </summary>
public static class ConfigStatus
{
    /// <summary>
    /// Indicates a configuration value is properly set
    /// </summary>
    public const string SET = "SET";
    
    /// <summary>
    /// Indicates a configuration value is not set
    /// </summary>
    public const string NOT_SET = "NOT SET";
}
```

### **4. Avoid Magic Numbers**
```csharp
// Bad
if (retryCount > 3) { ... }

// Good
private const int MAX_RETRY_ATTEMPTS = 3;
if (retryCount > MAX_RETRY_ATTEMPTS) { ... }
```

## Migration Guide

### **From Magic Strings to Constants**

**Before:**
```csharp
return Ok(new { status = "SET", message = "Success" });
```

**After:**
```csharp
return Ok(new { 
    status = AppConstants.ConfigStatus.SET, 
    message = AppConstants.Messages.SUCCESS 
});
```

### **Adding New Constants**

1. **Check if it's local or global**
2. **Add to appropriate constants file**
3. **Update all usages**
4. **Add XML documentation**
5. **Run SonarQube analysis**

## SonarQube Compliance

This approach ensures:
- ✅ **No magic string warnings**
- ✅ **Consistent naming conventions**
- ✅ **Proper code organization**
- ✅ **Maintainable codebase**

## Examples

### **Configuration Status**
```csharp
// Before
sftpHost = !string.IsNullOrEmpty(sftpHost) ? "SET" : "NOT SET"

// After
sftpHost = !string.IsNullOrEmpty(sftpHost) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET
```

### **Environment Names**
```csharp
// Before
if (environment == "Production") { ... }

// After
if (environment == AppConstants.Environment.PRODUCTION) { ... }
```

### **Storage Providers**
```csharp
// Before
if (provider == "s3") { ... }

// After
if (provider == AppConstants.StorageProvider.S3) { ... }
``` 