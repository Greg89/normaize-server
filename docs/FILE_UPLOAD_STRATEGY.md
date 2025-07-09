# File Upload & Storage Strategy for Normaize

## Overview

This document outlines the comprehensive file upload and storage strategy for the Normaize application, covering both backend and frontend implementations.

## Current Implementation

### Backend Architecture
- **File Upload Service**: Handles file validation, storage, and processing
- **Data Processing Service**: Manages dataset creation and metadata extraction
- **Storage Service**: Abstracted storage layer supporting multiple providers
- **Database**: MySQL with enhanced schema for dataset management

### Supported File Formats
- **CSV**: Comma-separated values with configurable delimiters
- **JSON**: Array of objects or single object format
- **Excel**: XLSX/XLS files with multiple worksheet support
- **XML**: Structured XML data with automatic element detection
- **TXT**: Plain text files (line-by-line processing)

## Storage Strategy Options

### Option 1: Hybrid Approach (Recommended) ⭐

**Architecture:**
- Original files stored in file system/cloud storage
- Processed data stored in database (inline for small datasets, separate table for large)
- Metadata and schema information in main dataset table

**Pros:**
- ✅ Original files preserved for audit/reprocessing
- ✅ Fast queries on processed data
- ✅ Database stays manageable
- ✅ Easy to regenerate data if needed
- ✅ Cost-effective for large files
- ✅ Flexible storage provider options

**Cons:**
- ❌ Storage cost (2x storage)
- ❌ Need to manage file cleanup
- ❌ Slightly more complex architecture

**Implementation:**
```csharp
// Small datasets (≤10K rows, ≤5MB)
dataSet.ProcessedData = JsonSerializer.Serialize(records);

// Large datasets (>10K rows, >5MB)
dataSet.UseSeparateTable = true;
// Data stored in DataSetRow table
```

### Option 2: Database-Only Approach

**Architecture:**
- All data stored in database tables
- Original file content in BLOB/LONGTEXT fields
- Processed data in structured tables

**Pros:**
- ✅ Single source of truth
- ✅ ACID transactions
- ✅ Easy backup/restore
- ✅ No external dependencies

**Cons:**
- ❌ Database size grows quickly
- ❌ Slower queries on large datasets
- ❌ More complex data management
- ❌ Higher database costs

### Option 3: Cloud Storage + Database

**Architecture:**
- Original files in cloud storage (S3, Azure, GCS)
- Processed data and metadata in database
- Cloud URLs stored for file access

**Pros:**
- ✅ Highly scalable
- ✅ Cost-effective for large files
- ✅ Global availability
- ✅ Built-in redundancy

**Cons:**
- ❌ Additional complexity
- ❌ Network dependencies
- ❌ Potential latency
- ❌ Vendor lock-in

## Enhanced Implementation Features

### 1. Smart Storage Strategy
```csharp
// Automatic decision based on dataset size
dataSet.UseSeparateTable = dataSet.RowCount > _maxRowsForInlineStorage || 
                          dataSet.FileSize > _maxFileSizeForInlineStorage;
```

### 2. Data Processing Pipeline
1. **File Validation**: Size, format, content validation
2. **File Storage**: Save to configured storage provider
3. **Data Extraction**: Parse file and extract structured data
4. **Schema Detection**: Automatic column/field detection
5. **Preview Generation**: First 10 rows for UI display
6. **Hash Generation**: For change detection and deduplication

### 3. Enhanced File Processing
- **CSV**: Configurable delimiters, header detection, encoding support
- **JSON**: Array and object format support, nested structure handling
- **Excel**: Multiple worksheet support, cell type detection
- **XML**: Automatic element detection, attribute handling
- **TXT**: Line-by-line processing with metadata

### 4. Storage Provider Abstraction
```csharp
public interface IStorageService
{
    Task<string> SaveFileAsync(FileUploadRequest fileRequest);
    Task<Stream> GetFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<string> GetFileUrlAsync(string filePath, TimeSpan? expiry = null);
}
```

## Frontend Implementation

### React File Upload Component
- **Drag & Drop**: Intuitive file upload interface
- **Progress Tracking**: Real-time upload progress
- **File Validation**: Client-side format and size validation
- **Multiple Formats**: Visual indicators for different file types
- **Error Handling**: Comprehensive error messages and retry options

### Features
- Support for multiple file formats with visual icons
- Real-time upload progress with progress bars
- Drag-and-drop interface with visual feedback
- File size and format validation
- Upload cancellation support
- Success/error state management

## Configuration Options

### File Upload Settings
```json
{
  "FileUpload": {
    "MaxFileSize": 104857600,
    "MaxRowsForInlineStorage": 10000,
    "MaxFileSizeForInlineStorage": 5242880,
    "AllowedExtensions": [".csv", ".json", ".xlsx", ".xls", ".xml", ".txt"],
    "ProcessingOptions": {
      "DefaultDelimiter": ",",
      "HasHeaderRecord": true,
      "MaxRowsToProcess": 10000
    }
  }
}
```

### Storage Configuration
```json
{
  "Storage": {
    "Provider": "Local",
    "Local": {
      "UploadPath": "uploads"
    },
    "S3": {
      "BucketName": "normaize-files",
      "Region": "us-east-1"
    }
  }
}
```

## Database Schema

### Enhanced DataSet Model
```csharp
public class DataSet
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string StorageProvider { get; set; }
    public string? ProcessedData { get; set; } // For small datasets
    public bool UseSeparateTable { get; set; } // For large datasets
    public string? DataHash { get; set; } // For change detection
    public List<DataSetRow> Rows { get; set; } = new();
}
```

### DataSetRow Model (for large datasets)
```csharp
public class DataSetRow
{
    public int Id { get; set; }
    public int DataSetId { get; set; }
    public int RowIndex { get; set; }
    public string Data { get; set; } // JSON serialized row data
    public DataSet DataSet { get; set; } = null!;
}
```

## Performance Considerations

### Database Optimization
- **Indexing**: Composite index on (DataSetId, RowIndex) for DataSetRow
- **Pagination**: Efficient data retrieval with skip/take
- **Batch Operations**: Bulk insert for large datasets
- **Connection Pooling**: Optimized database connections

### File Processing
- **Streaming**: Process files without loading entire content into memory
- **Parallel Processing**: Concurrent file processing for multiple uploads
- **Memory Management**: Efficient memory usage for large files
- **Caching**: Cache frequently accessed data

### Storage Optimization
- **Compression**: Automatic file compression for storage efficiency
- **Deduplication**: Hash-based file deduplication
- **Cleanup**: Automated cleanup of old/unused files
- **CDN**: Content delivery network for file access

## Security Considerations

### File Upload Security
- **File Type Validation**: Strict file extension and content validation
- **Size Limits**: Configurable file size limits
- **Virus Scanning**: Integration with antivirus services
- **Access Control**: User-based file access permissions

### Data Security
- **Encryption**: At-rest and in-transit encryption
- **Access Logging**: Comprehensive audit trails
- **Data Masking**: Sensitive data protection
- **Backup Security**: Encrypted backups

## Scalability Strategy

### Horizontal Scaling
- **Load Balancing**: Distribute upload requests across multiple servers
- **Database Sharding**: Partition data across multiple databases
- **CDN Integration**: Global file distribution
- **Microservices**: Separate file processing services

### Vertical Scaling
- **Resource Optimization**: Efficient memory and CPU usage
- **Database Optimization**: Query optimization and indexing
- **Storage Tiering**: Hot/cold storage strategies
- **Caching Layers**: Multi-level caching

## Monitoring and Analytics

### Metrics to Track
- **Upload Success Rate**: Percentage of successful uploads
- **Processing Time**: Average time to process files
- **Storage Usage**: File storage consumption
- **Error Rates**: Upload and processing errors
- **User Activity**: Upload patterns and usage

### Logging
- **Structured Logging**: Comprehensive error and activity logging
- **Performance Monitoring**: Upload and processing performance
- **Audit Trails**: Complete file access and modification logs
- **Alerting**: Automated alerts for issues

## Migration Strategy

### Phase 1: Enhanced Local Storage
1. Implement enhanced file processing
2. Add support for new file formats
3. Implement smart storage strategy
4. Add comprehensive error handling

### Phase 2: Cloud Storage Integration
1. Implement cloud storage providers
2. Add storage provider abstraction
3. Implement file migration tools
4. Add cloud-specific optimizations

### Phase 3: Advanced Features
1. Implement data deduplication
2. Add advanced analytics
3. Implement automated cleanup
4. Add performance optimizations

## Cost Analysis

### Storage Costs (Monthly)
- **Local Storage**: $0.05/GB (estimated)
- **S3 Standard**: $0.023/GB
- **Azure Blob**: $0.0184/GB
- **Database Storage**: $0.10/GB (estimated)

### Processing Costs
- **CPU/Memory**: $0.01-0.05 per GB processed
- **Network**: $0.09/GB (outbound)
- **CDN**: $0.085/GB

### Recommendations
- **Small Scale**: Local storage + database
- **Medium Scale**: S3 + database with CDN
- **Large Scale**: Multi-region cloud storage + database sharding

## Conclusion

The **Hybrid Approach** is recommended for the Normaize application because it provides:

1. **Flexibility**: Easy to switch between storage providers
2. **Performance**: Fast queries on processed data
3. **Cost-Effectiveness**: Optimal balance of storage and processing costs
4. **Scalability**: Can handle both small and large datasets efficiently
5. **Reliability**: Original files preserved for data recovery

This approach allows for future growth while maintaining good performance and cost efficiency for current needs. 