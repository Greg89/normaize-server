# Logging Constants Refactoring

## Overview

This document outlines the refactoring of logging message strings into centralized constants to improve maintainability, reduce duplication, and prevent typos.

## Problem

The codebase had multiple instances of the same logging message strings repeated across different services:

- `"Starting {Operation} for ID: {AnalysisId}. CorrelationId: {CorrelationId}"` (used 4 times in DataAnalysisService)
- `"Starting {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}"` (used 6 times in DataProcessingService)
- `"Starting {Operation} for ID: {DataSetId}, rows: {Rows}, user: {UserId}. CorrelationId: {CorrelationId}"` (used 1 time in DataProcessingService)

## Solution

### 1. Added Logging Constants to AppConstants.cs

Created a new `LogMessages` static class within `AppConstants.cs` to centralize all logging message templates:

```csharp
/// <summary>
/// Logging message templates
/// </summary>
public static class LogMessages
{
    public const string STARTING_OPERATION = "Starting {Operation} for ID: {AnalysisId}. CorrelationId: {CorrelationId}";
    public const string STARTING_OPERATION_WITH_USER = "Starting {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}";
    public const string STARTING_OPERATION_WITH_ROWS = "Starting {Operation} for ID: {DataSetId}, rows: {Rows}, user: {UserId}. CorrelationId: {CorrelationId}";
    public const string OPERATION_COMPLETED = "Operation {Operation} completed successfully. CorrelationId: {CorrelationId}";
    public const string OPERATION_FAILED = "Operation {Operation} failed. CorrelationId: {CorrelationId}";
}
```

### 2. Updated Services to Use Constants

#### DataAnalysisService.cs
- Added `using Normaize.Core.Constants;`
- Replaced 4 instances of the repeated string with `AppConstants.LogMessages.STARTING_OPERATION`

#### DataProcessingService.cs
- Added `using Normaize.Core.Constants;`
- Replaced 6 instances of the repeated string with `AppConstants.LogMessages.STARTING_OPERATION_WITH_USER`
- Replaced 1 instance of the rows-specific string with `AppConstants.LogMessages.STARTING_OPERATION_WITH_ROWS`

## Benefits

1. **Maintainability**: Changes to log message formats only need to be made in one place
2. **Consistency**: Ensures all services use the same message format
3. **Reduced Duplication**: Eliminates repeated string literals
4. **Type Safety**: Compile-time checking prevents typos in message templates
5. **Centralized Management**: All logging constants are in one location for easy discovery

## Files Modified

- `Normaize.Core/Constants/AppConstants.cs` - Added LogMessages class
- `Normaize.Core/Services/DataAnalysisService.cs` - Updated to use constants
- `Normaize.Core/Services/DataProcessingService.cs` - Updated to use constants

## Testing

- All existing tests continue to pass
- Build succeeds without errors
- No functional changes to logging behavior

## Future Improvements

Additional logging constants can be added to the `LogMessages` class as needed:

- Error message templates
- Success message templates
- Warning message templates
- Audit log message templates

This pattern can be extended to other services in the codebase that have similar logging message duplication. 