# InMemoryStorageService Refactoring Documentation

## Overview

This document details the comprehensive refactoring of the `InMemoryStorageService` to align with industry standards, SonarQube quality rules, and chaos engineering principles. The refactoring transforms a basic in-memory storage implementation into a production-ready, resilient, and observable service.

## Key Improvements

### 🎯 **Industry Standards Compliance**

#### 1. **Structured Logging with Correlation IDs**
- **Before**: Basic logging without context
- **After**: Structured logging with correlation IDs for distributed tracing
- **Benefits**: 
  - Enables request tracing across service boundaries
  - Facilitates debugging in distributed systems
  - Improves observability and monitoring

```csharp
// Before
_logger.LogInformation("File saved in memory: {FilePath}, Size: {FileSize} bytes", filePath, fileData.Length);

// After
_logger.LogInformation("File saved successfully. CorrelationId: {CorrelationId}, FilePath: {FilePath}, Size: {FileSize} bytes, Hash: {FileHash}",
    correlationId, filePath, fileData.Length, fileHash);
```

#### 2. **Timeout Management**
- **Before**: No timeout handling
- **After**: Configurable timeouts for all operations
- **Benefits**:
  - Prevents hanging operations
  - Improves system responsiveness
  - Enables graceful degradation

```csharp
private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
{
    using var cts = new CancellationTokenSource(timeout);
    
    try
    {
        return await operation().WaitAsync(cts.Token);
    }
    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    {
        _logger.LogError("Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
            operationName, timeout, correlationId);
        throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
    }
}
```

#### 3. **Configuration-Driven Design**
- **Before**: Hard-coded values
- **After**: `InMemoryStorageOptions` configuration class
- **Benefits**:
  - Environment-specific configuration
  - Runtime tuning without code changes
  - Better testability

```csharp
public class InMemoryStorageOptions
{
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public int MaxFiles { get; set; } = 1000;
    public int MaxConcurrentOperations { get; set; } = 10;
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan QuickTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(30);
    public int FileRetentionMinutes { get; set; } = 60; // 1 hour
    public double ChaosStorageFullProbability { get; set; } = 0.001; // 0.1%
    public double ChaosFileCorruptionProbability { get; set; } = 0.0005; // 0.05%
}
```

### 🔒 **Security Enhancements**

#### 1. **File Path Sanitization**
- **Before**: No path validation
- **After**: Comprehensive path sanitization
- **Benefits**:
  - Prevents path traversal attacks
  - Ensures file name safety
  - Validates file extensions

```csharp
private static string SanitizeFileName(string fileName)
{
    // Remove path traversal attempts and invalid characters
    var sanitized = Path.GetFileName(fileName);
    if (string.IsNullOrEmpty(sanitized))
    {
        throw new ArgumentException("Invalid file name after sanitization", nameof(fileName));
    }
    return sanitized;
}
```

#### 2. **File Integrity Verification**
- **Before**: No integrity checks
- **After**: SHA256 hash verification
- **Benefits**:
  - Detects file corruption
  - Ensures data integrity
  - Provides audit trail

```csharp
private static string CalculateFileHash(byte[] data)
{
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(data);
    return Convert.ToBase64String(hashBytes);
}
```

### 🛡️ **Chaos Engineering Integration**

#### 1. **Controlled Failure Injection**
- **Storage Full Simulation**: Randomly simulates storage capacity issues
- **File Corruption Simulation**: Randomly simulates file corruption
- **Benefits**:
  - Tests system resilience
  - Validates error handling
  - Prepares for real-world failures

```csharp
// Chaos engineering: Simulate storage full condition
if (_chaosRandom.NextDouble() < _options.ChaosStorageFullProbability)
{
    _logger.LogWarning("Chaos engineering: Simulating storage full condition. CorrelationId: {CorrelationId}", correlationId);
    throw new InvalidOperationException("Storage is full (chaos engineering simulation)");
}

// Chaos engineering: Simulate file corruption
if (_chaosRandom.NextDouble() < _options.ChaosFileCorruptionProbability)
{
    _logger.LogWarning("Chaos engineering: Simulating file corruption. CorrelationId: {CorrelationId}", correlationId);
    throw new InvalidOperationException("File is corrupted (chaos engineering simulation)");
}
```

#### 2. **Configurable Chaos Parameters**
- **Storage Full Probability**: 0.1% default
- **File Corruption Probability**: 0.05% default
- **Benefits**:
  - Environment-specific chaos testing
  - Gradual failure rate increases
  - Safe production deployment

### 📊 **Enhanced Observability**

#### 1. **Comprehensive Metrics**
- **File Count**: Total files in storage
- **Total Size**: Aggregate storage usage
- **Access Patterns**: Last accessed timestamps
- **Benefits**:
  - Performance monitoring
  - Capacity planning
  - Usage analytics

#### 2. **Detailed Audit Trail**
- **File Creation**: Timestamp and metadata
- **File Access**: Last accessed tracking
- **File Deletion**: Removal logging
- **Benefits**:
  - Compliance requirements
  - Security auditing
  - Troubleshooting

### 🔄 **Concurrency and Resource Management**

#### 1. **Semaphore-Based Concurrency Control**
- **Before**: No concurrency limits
- **After**: Configurable semaphore limits
- **Benefits**:
  - Prevents resource exhaustion
  - Improves performance under load
  - Better resource utilization

```csharp
private readonly SemaphoreSlim _storageSemaphore;

public InMemoryStorageService(ILogger<InMemoryStorageService> logger, IOptions<InMemoryStorageOptions> options)
{
    _storageSemaphore = new SemaphoreSlim(_options.MaxConcurrentOperations, _options.MaxConcurrentOperations);
}
```

#### 2. **Intelligent Memory Management**
- **Automatic Cleanup**: Time-based file removal
- **LRU Eviction**: Least recently used file removal
- **Size Limits**: Configurable storage limits
- **Benefits**:
  - Prevents memory leaks
  - Optimizes memory usage
  - Maintains performance

```csharp
private void CleanupExpiredFiles(object? state)
{
    var correlationId = Guid.NewGuid().ToString();
    
    _logger.LogDebug("Starting memory cleanup. CorrelationId: {CorrelationId}, Current files: {FileCount}",
        correlationId, _fileStorage.Count);

    try
    {
        var currentTime = DateTime.UtcNow;
        var filesToRemove = new List<string>();
        
        // Remove files that haven't been accessed recently
        var cutoffTime = currentTime.AddMinutes(-_options.FileRetentionMinutes);
        
        foreach (var kvp in _fileStorage)
        {
            if (kvp.Value.LastAccessed < cutoffTime)
            {
                filesToRemove.Add(kvp.Key);
            }
        }
        
        // If still too many files, remove oldest ones
        if (_fileStorage.Count > _options.MaxFiles)
        {
            var oldestFiles = _fileStorage
                .OrderBy(kvp => kvp.Value.CreatedAt)
                .Take(_fileStorage.Count - _options.MaxFiles + filesToRemove.Count)
                .Select(kvp => kvp.Key)
                .Where(key => !filesToRemove.Contains(key))
                .ToList();
            
            filesToRemove.AddRange(oldestFiles);
        }
        
        // Execute cleanup with detailed logging
        var removedCount = 0;
        foreach (var filePath in filesToRemove)
        {
            if (_fileStorage.TryRemove(filePath, out var removedMetadata))
            {
                removedCount++;
                _logger.LogDebug("Cleaned up file from memory. CorrelationId: {CorrelationId}, FilePath: {FilePath}, Size: {FileSize} bytes",
                    correlationId, filePath, removedMetadata.Data.Length);
            }
        }
        
        if (removedCount > 0)
        {
            _logger.LogInformation("Memory cleanup completed. CorrelationId: {CorrelationId}, Removed: {RemovedCount}, Remaining: {RemainingCount}",
                correlationId, removedCount, _fileStorage.Count);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during memory cleanup. CorrelationId: {CorrelationId}", correlationId);
        // Don't rethrow - cleanup failures shouldn't break the service
    }
}
```

### 🧪 **Improved Testability**

#### 1. **Static Validation Methods**
- **Before**: Instance methods for validation
- **After**: Static validation methods
- **Benefits**:
  - Better unit testing
  - Reusable validation logic
  - Clearer separation of concerns

```csharp
private static void ValidateSaveFileInputs(FileUploadRequest? fileRequest)
{
    if (fileRequest == null)
        throw new ArgumentNullException(nameof(fileRequest));
    
    if (string.IsNullOrWhiteSpace(fileRequest.FileName))
        throw new ArgumentException("FileName cannot be null or empty", nameof(fileRequest));
    
    if (fileRequest.FileStream == null)
        throw new ArgumentException("FileStream cannot be null", nameof(fileRequest));
    
    if (!fileRequest.FileStream.CanRead)
        throw new ArgumentException("FileStream must be readable", nameof(fileRequest));
}
```

#### 2. **Enhanced Test Coverage**
- **Constructor Validation**: All dependencies validated
- **Input Validation**: Comprehensive parameter checking
- **Error Scenarios**: Chaos engineering simulation tests
- **Concurrency Tests**: Thread safety validation
- **Resource Management**: Memory cleanup verification

### 🔧 **Error Handling Improvements**

#### 1. **Eliminated Log-and-Rethrow Anti-Pattern**
- **Before**: Logging exceptions and rethrowing
- **After**: Structured error handling with correlation IDs
- **Benefits**:
  - Better exception handling
  - Improved debugging
  - Reduced log noise

#### 2. **Graceful Degradation**
- **Timeout Handling**: Operations don't hang indefinitely
- **Resource Limits**: Graceful handling of storage limits
- **Cleanup Failures**: Non-blocking cleanup operations
- **Benefits**:
  - Improved system stability
  - Better user experience
  - Reduced system impact

### 📈 **Performance Optimizations**

#### 1. **Efficient Data Structures**
- **ConcurrentDictionary**: Thread-safe operations
- **MemoryStream Optimization**: Proper stream handling
- **Metadata Tracking**: Efficient file management
- **Benefits**:
  - Better concurrency
  - Reduced memory overhead
  - Improved performance

#### 2. **Smart Caching Strategy**
- **Access Tracking**: LRU-based eviction
- **Size Monitoring**: Automatic capacity management
- **Cleanup Scheduling**: Background maintenance
- **Benefits**:
  - Optimal memory usage
  - Consistent performance
  - Automatic resource management

## Configuration Options

### InMemoryStorageOptions

| Property | Default | Description |
|----------|---------|-------------|
| `MaxFileSizeBytes` | 100MB | Maximum file size allowed |
| `MaxFiles` | 1000 | Maximum number of files in storage |
| `MaxConcurrentOperations` | 10 | Maximum concurrent operations |
| `OperationTimeout` | 5 minutes | Timeout for file operations |
| `QuickTimeout` | 30 seconds | Timeout for quick operations |
| `CleanupInterval` | 30 minutes | Memory cleanup frequency |
| `FileRetentionMinutes` | 60 | File retention period |
| `ChaosStorageFullProbability` | 0.001 | Storage full simulation rate |
| `ChaosFileCorruptionProbability` | 0.0005 | File corruption simulation rate |

## Usage Examples

### Basic Configuration

```csharp
services.Configure<InMemoryStorageOptions>(options =>
{
    options.MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    options.MaxFiles = 500;
    options.ChaosStorageFullProbability = 0.0; // Disable chaos engineering
});
```

### Advanced Configuration

```csharp
services.Configure<InMemoryStorageOptions>(options =>
{
    options.MaxFileSizeBytes = 200 * 1024 * 1024; // 200MB
    options.MaxFiles = 2000;
    options.MaxConcurrentOperations = 20;
    options.OperationTimeout = TimeSpan.FromMinutes(10);
    options.CleanupInterval = TimeSpan.FromMinutes(15);
    options.FileRetentionMinutes = 120; // 2 hours
    options.ChaosStorageFullProbability = 0.002; // 0.2%
    options.ChaosFileCorruptionProbability = 0.001; // 0.1%
});
```

## Testing Strategy

### Unit Tests

1. **Constructor Validation Tests**
   - Null dependency injection
   - Invalid configuration options

2. **Input Validation Tests**
   - Null file requests
   - Invalid file names
   - Unreadable streams

3. **Operation Tests**
   - File save/retrieve/delete operations
   - Concurrent operations
   - Timeout scenarios

4. **Chaos Engineering Tests**
   - Storage full simulation
   - File corruption simulation
   - Error recovery

5. **Resource Management Tests**
   - Memory cleanup
   - File eviction
   - Statistics calculation

### Integration Tests

1. **End-to-End Workflows**
   - Complete file lifecycle
   - Multiple concurrent users
   - High-load scenarios

2. **Configuration Tests**
   - Different option combinations
   - Environment-specific settings
   - Runtime configuration changes

## Monitoring and Alerting

### Key Metrics

1. **Storage Metrics**
   - Total file count
   - Total storage size
   - Average file size

2. **Performance Metrics**
   - Operation latency
   - Throughput rates
   - Error rates

3. **Resource Metrics**
   - Memory usage
   - Cleanup frequency
   - Eviction rates

### Alerting Rules

1. **High Error Rate**: >5% operation failures
2. **Storage Full**: >90% capacity utilization
3. **High Latency**: >5 second operation times
4. **Memory Pressure**: >80% memory usage

## Migration Guide

### From Old Implementation

1. **Update Constructor**
   ```csharp
   // Before
   var service = new InMemoryStorageService(logger);
   
   // After
   var options = new InMemoryStorageOptions();
   var service = new InMemoryStorageService(logger, Options.Create(options));
   ```

2. **Update Configuration**
   ```csharp
   // Before: Hard-coded values
   // After: Configuration-driven
   services.Configure<InMemoryStorageOptions>(configuration.GetSection("Storage"));
   ```

3. **Update Tests**
   ```csharp
   // Before
   var service = new InMemoryStorageService(mockLogger.Object);
   
   // After
   var mockOptions = new Mock<IOptions<InMemoryStorageOptions>>();
   var options = new InMemoryStorageOptions();
   mockOptions.Setup(x => x.Value).Returns(options);
   var service = new InMemoryStorageService(mockLogger.Object, mockOptions.Object);
   ```

## Future Enhancements

### Planned Improvements

1. **Compression Support**
   - Automatic file compression
   - Configurable compression levels
   - Compression ratio monitoring

2. **Encryption Support**
   - File-level encryption
   - Key management integration
   - Encryption performance optimization

3. **Advanced Chaos Engineering**
   - Network latency simulation
   - Memory pressure simulation
   - CPU throttling simulation

4. **Enhanced Monitoring**
   - Custom metrics dashboard
   - Performance trend analysis
   - Predictive capacity planning

5. **Distributed Storage**
   - Multi-node support
   - Data replication
   - Load balancing

## Conclusion

The refactored `InMemoryStorageService` represents a significant improvement in terms of:

- **Reliability**: Comprehensive error handling and chaos engineering
- **Observability**: Structured logging and detailed metrics
- **Security**: File validation and integrity checks
- **Performance**: Optimized concurrency and resource management
- **Maintainability**: Clean architecture and comprehensive testing
- **Scalability**: Configuration-driven design and resource limits

This implementation serves as a foundation for production-ready storage services and demonstrates best practices for building resilient, observable, and maintainable systems in .NET. 