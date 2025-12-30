# Data Normalization Feature

## Overview

The Data Normalization feature provides a robust, scalable solution for processing large datasets without blocking user requests. This feature implements a queue-based architecture that allows users to submit normalization jobs and monitor their progress asynchronously.

## Architecture

### Core Components

1. **Data Normalization Service** (`IDataNormalizationService`)
   - Main service for coordinating normalization operations
   - Handles job submission, status queries, and job management
   - Validates user permissions and dataset access

2. **Job Queue Service** (`IJobQueueService`)
   - Database-backed job queue for reliable job storage
   - Manages job lifecycle (queued → processing → completed/failed)
   - Implements priority-based job processing
   - Handles job retries with exponential backoff

3. **Duplicate Row Removal Processor** (`IDuplicateRowRemovalProcessor`)
   - Specialized processor for removing duplicate rows
   - Handles case-sensitive and case-insensitive duplicate detection
   - Supports configurable column selection for duplicate determination
   - Provides progress updates during processing

4. **Background Service** (`DataNormalizationBackgroundService`)
   - Continuously processes jobs from the queue
   - Runs independently of HTTP requests
   - Handles job failures and retries
   - Provides real-time progress updates

### Data Flow

```
Client Request → API Controller → Normalization Service → Job Queue → Background Service → Processor → Database Update
```

## Features

### Duplicate Row Removal

The first normalization operation implemented is **Duplicate Row Removal**, which allows users to:

- **Specify columns**: Choose which columns to use for duplicate detection
- **Control case sensitivity**: Option to consider or ignore letter casing
- **Choose retention strategy**: Keep first or last occurrence of duplicates
- **Monitor progress**: Real-time progress updates during processing
- **Handle large datasets**: Efficient processing of datasets with millions of rows

### Job Management

- **Asynchronous processing**: Submit jobs and continue with other tasks
- **Progress tracking**: Real-time progress updates (0-100%)
- **Job status monitoring**: Check job status at any time
- **Job cancellation**: Cancel jobs that are queued or processing
- **Retry mechanism**: Automatic retry of failed jobs with exponential backoff
- **Audit trail**: Complete history of all job operations

### Scalability Features

- **Queue-based processing**: Jobs are processed in the background
- **Concurrent processing**: Multiple jobs can be processed simultaneously
- **Priority queuing**: High-priority jobs are processed first
- **Resource management**: Memory and processing time estimation
- **Cleanup automation**: Automatic cleanup of old completed jobs

## API Endpoints

### Submit Duplicate Row Removal Job

```http
POST /api/datanormalization/datasets/{dataSetId}/remove-duplicates
```

**Request Body:**
```json
{
  "columnNames": ["email", "phone"],
  "keepFirstOccurrence": true,
  "caseSensitive": false
}
```

**Response:**
```json
{
  "jobId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Queued",
  "message": "Normalization job queued for processing",
  "submittedAt": "2024-01-15T10:30:00Z",
  "estimatedCompletionAt": "2024-01-15T10:35:00Z",
  "progressPercentage": 0,
  "success": true
}
```

### Get Job Status

```http
GET /api/datanormalization/jobs/{jobId}
```

**Response:**
```json
{
  "jobId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Processing",
  "message": "Normalization job started processing",
  "submittedAt": "2024-01-15T10:30:00Z",
  "startedAt": "2024-01-15T10:30:05Z",
  "completedAt": null,
  "progressPercentage": 45,
  "errorMessage": null,
  "results": null
}
```

### Cancel Job

```http
POST /api/datanormalization/jobs/{jobId}/cancel
```

### Get User Jobs

```http
GET /api/datanormalization/jobs?page=1&pageSize=20&includeCompleted=false
```

### Get Dataset Jobs

```http
GET /api/datanormalization/datasets/{dataSetId}/jobs
```

## Configuration

### Job Queue Options

```json
{
  "JobQueue": {
    "MaxConcurrentJobs": 5,
    "CleanupInterval": "01:00:00",
    "RetryCheckInterval": "00:05:00",
    "JobRetentionDays": 30
  }
}
```

### Background Service Options

```json
{
  "DataNormalizationBackgroundService": {
    "IdleDelay": "00:00:10",
    "ErrorRetryDelay": "00:00:30",
    "MaxConcurrentProcessors": 3
  }
}
```

## Usage Examples

### Client-Side Implementation

```csharp
// Submit a normalization job
var request = new RemoveDuplicateRowsRequest
{
    ColumnNames = new[] { "email", "phone" },
    KeepFirstOccurrence = true,
    CaseSensitive = false
};

var response = await httpClient.PostAsJsonAsync(
    $"/api/datanormalization/datasets/{datasetId}/remove-duplicates", 
    request);

var jobResponse = await response.Content.ReadFromJsonAsync<NormalizationJobResponse>();

// Poll for job status
while (true)
{
    var statusResponse = await httpClient.GetFromJsonAsync<NormalizationJobStatusResponse>(
        $"/api/datanormalization/jobs/{jobResponse.JobId}");
    
    if (statusResponse.Status == NormalizationJobStatus.Completed)
    {
        Console.WriteLine($"Job completed! Removed {statusResponse.Results.DuplicateRowsRemoved} duplicates");
        break;
    }
    
    if (statusResponse.Status == NormalizationJobStatus.Failed)
    {
        Console.WriteLine($"Job failed: {statusResponse.ErrorMessage}");
        break;
    }
    
    Console.WriteLine($"Progress: {statusResponse.ProgressPercentage}% - {statusResponse.Message}");
    await Task.Delay(2000); // Wait 2 seconds before checking again
}
```

### Server-Side Implementation

```csharp
// In your service
public async Task<NormalizationJobResponse> RemoveDuplicatesAsync(
    int dataSetId, 
    RemoveDuplicateRowsRequest request, 
    string userId)
{
    return await _normalizationService.SubmitDuplicateRowRemovalJobAsync(
        dataSetId, request, userId);
}

// In your background service
public async Task ProcessJobsAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var job = await _jobQueueService.DequeueJobAsync(cancellationToken);
        if (job != null)
        {
            await ProcessJobAsync(job, cancellationToken);
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}
```

## Database Schema

### DataNormalizationJob Table

```sql
CREATE TABLE DataNormalizationJobs (
    Id NVARCHAR(450) PRIMARY KEY,
    DataSetId INT NOT NULL,
    UserId NVARCHAR(255) NOT NULL,
    OperationType NVARCHAR(100) NOT NULL,
    OperationParameters NVARCHAR(MAX),
    Status INT NOT NULL,
    Priority INT NOT NULL DEFAULT 1,
    SubmittedAt DATETIME2 NOT NULL,
    StartedAt DATETIME2,
    CompletedAt DATETIME2,
    ProgressPercentage INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(2000),
    Results NVARCHAR(MAX),
    RetryCount INT NOT NULL DEFAULT 0,
    MaxRetries INT NOT NULL DEFAULT 3,
    NextRetryAt DATETIME2,
    CorrelationId NVARCHAR(255),
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2,
    DeletedBy NVARCHAR(255),
    LastModifiedAt DATETIME2 NOT NULL,
    LastModifiedBy NVARCHAR(255),
    FOREIGN KEY (DataSetId) REFERENCES DataSets(Id)
);
```

### DataNormalizationAuditLog Table

```sql
CREATE TABLE DataNormalizationAuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NormalizationJobId NVARCHAR(450) NOT NULL,
    UserId NVARCHAR(255) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    Changes NVARCHAR(MAX),
    Timestamp DATETIME2 NOT NULL,
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(MAX),
    FOREIGN KEY (NormalizationJobId) REFERENCES DataNormalizationJobs(Id)
);
```

## Performance Considerations

### Memory Management

- **Batch processing**: Large datasets are processed in configurable batches
- **Memory estimation**: Pre-processing memory usage estimation
- **Resource limits**: Configurable memory and processing time limits
- **Garbage collection**: Efficient memory cleanup during processing

### Processing Optimization

- **Column indexing**: Efficient duplicate detection using optimized data structures
- **Progress reporting**: Minimal overhead for progress updates
- **Cancellation support**: Responsive job cancellation
- **Error handling**: Graceful degradation and recovery

### Scalability

- **Concurrent processing**: Multiple jobs processed simultaneously
- **Queue prioritization**: High-priority jobs processed first
- **Resource pooling**: Efficient resource utilization
- **Horizontal scaling**: Support for multiple background service instances

## Security Features

- **User isolation**: Users can only access their own jobs and datasets
- **Input validation**: Comprehensive validation of all input parameters
- **Audit logging**: Complete audit trail of all operations
- **Rate limiting**: Configurable rate limiting for job submissions
- **Authentication required**: All endpoints require valid authentication

## Monitoring and Observability

### Logging

- **Structured logging**: Consistent log format across all components
- **Correlation IDs**: Track requests across service boundaries
- **Performance metrics**: Processing time and memory usage logging
- **Error tracking**: Detailed error logging with context

### Health Checks

- **Queue health**: Monitor queue length and processing status
- **Service health**: Background service status monitoring
- **Database health**: Job storage and retrieval health checks
- **Performance metrics**: Response time and throughput monitoring

## Future Enhancements

### Planned Normalization Operations

1. **Data Type Standardization**
   - Convert data types to consistent formats
   - Handle date/time normalization
   - Standardize number formats

2. **Missing Value Handling**
   - Fill missing values with defaults
   - Interpolate missing values
   - Remove rows with critical missing data

3. **Data Validation**
   - Validate data against schemas
   - Check data ranges and constraints
   - Identify data quality issues

4. **Data Cleansing**
   - Remove special characters
   - Standardize text formats
   - Handle encoding issues

### Advanced Features

1. **Machine Learning Integration**
   - Intelligent duplicate detection
   - Pattern-based data cleaning
   - Automated quality assessment

2. **Real-time Processing**
   - Stream processing capabilities
   - Real-time data validation
   - Live quality monitoring

3. **Batch Operations**
   - Multiple dataset processing
   - Chained normalization operations
   - Dependency management

## Troubleshooting

### Common Issues

1. **Job Stuck in Processing**
   - Check background service logs
   - Verify database connectivity
   - Check for long-running operations

2. **Memory Issues**
   - Reduce batch size
   - Increase memory limits
   - Process smaller datasets

3. **Performance Issues**
   - Check queue length
   - Monitor concurrent job count
   - Review database performance

### Debug Information

- **Job correlation IDs**: Track jobs across all components
- **Detailed error messages**: Comprehensive error information
- **Progress tracking**: Real-time processing status
- **Audit logs**: Complete operation history

## Conclusion

The Data Normalization feature provides a robust, scalable solution for processing large datasets asynchronously. The queue-based architecture ensures that users can submit normalization requests without waiting for completion, while the comprehensive job management system provides full visibility into the processing status.

This implementation follows clean architecture principles, provides excellent separation of concerns, and is designed for future extensibility. The modular design makes it easy to add new normalization operations while maintaining the existing infrastructure.
