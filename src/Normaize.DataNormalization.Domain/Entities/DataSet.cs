using System;
using System.Collections.Generic;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Domain.Events;

namespace Normaize.DataNormalization.Domain.Entities;

/// <summary>
/// DataSet aggregate root for the Data Normalization bounded context
/// Manages the lifecycle of uploaded datasets including processing, retention, and deletion
/// </summary>
public class DataSet
{
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string UserId { get; private set; }
    public FileMetadata FileInfo { get; private set; }
    public DatasetStatistics Statistics { get; private set; }
    public ProcessingStatus ProcessingStatus { get; private set; }
    public RetentionPolicy? RetentionPolicy { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public string? Schema { get; private set; }
    public string? PreviewData { get; private set; }
    public string? ProcessedData { get; private set; }
    public string? ProcessingErrors { get; private set; }
    
    // Legacy compatibility - use RetentionPolicy.ExpiryDate instead
    public DateTime? RetentionExpiryDate => RetentionPolicy?.ExpiryDate;

    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    // Audit trail
    public DateTime LastModifiedAt { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    private DataSet() // EF Core constructor
    {
        Name = string.Empty;
        UserId = string.Empty;
        FileInfo = null!;
        Statistics = null!;
        ProcessingStatus = null!;
    }

    private DataSet(
        string name,
        string? description,
        string userId,
        FileMetadata fileInfo,
        DatasetStatistics statistics,
        RetentionPolicy? retentionPolicy = null)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        ProcessingStatus = ValueObjects.ProcessingStatus.Pending();
        RetentionPolicy = retentionPolicy ?? ValueObjects.RetentionPolicy.Default();
        UploadedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        IsDeleted = false;

        AddDomainEvent(new DataSetUploadedEvent(UserId, name, fileInfo.FileName, fileInfo.FileSize));
    }

    /// <summary>
    /// Creates a new dataset
    /// </summary>
    public static DataSet Create(
        string name,
        string? description,
        string userId,
        FileMetadata fileInfo,
        DatasetStatistics? statistics = null,
        int? retentionDays = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Dataset name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var datasetStats = statistics ?? DatasetStatistics.Empty;
        var retentionPolicy = retentionDays.HasValue 
            ? ValueObjects.RetentionPolicy.Create(retentionDays.Value)
            : ValueObjects.RetentionPolicy.Default();

        return new DataSet(name.Trim(), description?.Trim(), userId.Trim(), fileInfo, datasetStats, retentionPolicy);
    }

    /// <summary>
    /// Updates the dataset metadata (name and description)
    /// </summary>
    public void UpdateMetadata(string name, string? description, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Dataset name cannot be null or empty", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        UpdateModificationInfo(modifiedBy);
        
        _domainEvents.Add(new DataSetMetadataUpdatedEvent(Id, Name, Description));
    }

    /// <summary>
    /// Updates the dataset schema information
    /// </summary>
    public void UpdateSchema(string schema, string? modifiedBy = null)
    {
        Schema = schema;
        UpdateModificationInfo(modifiedBy);
    }

    /// <summary>
    /// Updates the dataset statistics after processing
    /// </summary>
    public void UpdateStatistics(DatasetStatistics newStatistics, string? modifiedBy = null)
    {
        Statistics = newStatistics ?? throw new ArgumentNullException(nameof(newStatistics));
        UpdateModificationInfo(modifiedBy);
    }

    /// <summary>
    /// Marks dataset as successfully processed with schema and preview data
    /// </summary>
    public void MarkAsProcessedWithDetails(string schema, int rowCount, int columnCount, string? previewData, string? modifiedBy = null)
    {
        if (ProcessingStatus.IsProcessed)
            throw new InvalidOperationException("Dataset has already been processed");

        if (columnCount <= 0)
            throw new ArgumentException("Column count must be positive", nameof(columnCount));

        Schema = schema;
        PreviewData = previewData;
        
        // Update statistics with actual counts
        var updatedStats = new DatasetStatistics(
            rowCount,
            columnCount,
            true,
            Statistics.UseSeparateTable,
            DateTime.UtcNow);
        
        Statistics = updatedStats;
        ProcessingStatus = ValueObjects.ProcessingStatus.Completed();
        UpdateModificationInfo(modifiedBy);

        AddDomainEvent(new DataSetProcessedEvent(Id, UserId, rowCount, columnCount));
    }

    /// <summary>
    /// Marks processing as failed with error message
    /// </summary>
    public void MarkProcessingAsFailed(string error, string? modifiedBy = null)
    {
        ProcessingStatus = ValueObjects.ProcessingStatus.Failed(error);
        ProcessingErrors = error;
        UpdateModificationInfo(modifiedBy);

        AddDomainEvent(new DataSetProcessingFailedEvent(Id, UserId, error));
    }

    /// <summary>
    /// Sets the preview data for the dataset
    /// </summary>
    public void SetPreviewData(string previewData, string? modifiedBy = null)
    {
        PreviewData = previewData;
        UpdateModificationInfo(modifiedBy);
    }

    /// <summary>
    /// Sets the processed data for small datasets
    /// </summary>
    public void SetProcessedData(string processedData, string? modifiedBy = null)
    {
        ProcessedData = processedData;
        Statistics = Statistics.MarkAsProcessed();
        ProcessingStatus = ValueObjects.ProcessingStatus.Completed();
        UpdateModificationInfo(modifiedBy);
    }

    /// <summary>
    /// Records processing errors
    /// </summary>
    public void RecordProcessingError(string error, string? modifiedBy = null)
    {
        ProcessingErrors = error;
        UpdateModificationInfo(modifiedBy);
    }

    /// <summary>
    /// Updates the file metadata (e.g., after moving to different storage)
    /// </summary>
    public void UpdateFileInfo(FileMetadata newFileInfo, string? modifiedBy = null)
    {
        FileInfo = newFileInfo ?? throw new ArgumentNullException(nameof(newFileInfo));
        UpdateModificationInfo(modifiedBy);
    }

    /// <summary>
    /// Sets the retention expiry date for the dataset
    /// </summary>
    public void SetRetentionPolicy(DateTime expiryDate, string? modifiedBy = null)
    {
        if (expiryDate <= DateTime.UtcNow)
            throw new ArgumentException("Retention expiry date must be in the future", nameof(expiryDate));

        RetentionPolicy = ValueObjects.RetentionPolicy.CreateWithExpiryDate(expiryDate);
        UpdateModificationInfo(modifiedBy);

        AddDomainEvent(new RetentionPolicyUpdatedEvent(Id, UserId, RetentionPolicy.RetentionDays, expiryDate));
    }

    /// <summary>
    /// Updates retention policy with new retention days
    /// </summary>
    public void UpdateRetentionPolicy(int retentionDays, string? modifiedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update retention policy for a deleted dataset");

        RetentionPolicy = ValueObjects.RetentionPolicy.Create(retentionDays);
        UpdateModificationInfo(modifiedBy);

        AddDomainEvent(new RetentionPolicyUpdatedEvent(Id, UserId, retentionDays, RetentionPolicy.ExpiryDate));
    }

    /// <summary>
    /// Soft deletes the dataset
    /// </summary>
    public void Delete(string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));

        if (IsDeleted)
            throw new InvalidOperationException("Dataset is already deleted");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdateModificationInfo(deletedBy);

        AddDomainEvent(new DataSetDeletedEvent(Id, UserId, deletedBy));
    }

    /// <summary>
    /// Restores a soft-deleted dataset
    /// </summary>
    public void Restore(string restoredBy)
    {
        if (string.IsNullOrWhiteSpace(restoredBy))
            throw new ArgumentException("RestoredBy cannot be null or empty", nameof(restoredBy));

        if (!IsDeleted)
            throw new InvalidOperationException("Dataset is not deleted");

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdateModificationInfo(restoredBy);

        AddDomainEvent(new DataSetRestoredEvent(Id, UserId));
    }

    /// <summary>
    /// Resets dataset to original unprocessed state
    /// </summary>
    public void ResetToOriginal(string? modifiedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot reset a deleted dataset");

        ProcessingStatus = ValueObjects.ProcessingStatus.Pending();
        Schema = null;
        PreviewData = null;
        ProcessedData = null;
        ProcessingErrors = null;
        
        // Reset statistics to unprocessed state
        Statistics = new DatasetStatistics(
            0,
            0,
            false,
            Statistics.UseSeparateTable);
        
        UpdateModificationInfo(modifiedBy);

        AddDomainEvent(new DataSetResetEvent(Id, UserId));
    }

    // Business rule queries
    public bool IsRetentionExpired => RetentionPolicy?.IsExpired ?? false;
    public bool IsProcessed => ProcessingStatus.IsProcessed;
    public bool IsLargeDataset => Statistics.IsLargeDataset;
    public bool RequiresSeparateTable => Statistics.UseSeparateTable;
    public bool HasProcessingErrors => ProcessingStatus.HasError || !string.IsNullOrWhiteSpace(ProcessingErrors);
    public bool IsTextBasedFile => FileInfo.FileType.IsTextBased;
    public bool IsStoredInCloud => FileInfo.StorageProvider.IsCloudBased;

    /// <summary>
    /// Ensures the user has access to this dataset
    /// </summary>
    public void EnsureUserAccess(string userId)
    {
        if (UserId != userId)
            throw new UnauthorizedAccessException($"User {userId} does not have access to dataset {Id}");
    }

    private void UpdateModificationInfo(string? modifiedBy)
    {
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    private void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Marks the dataset as processed (for compatibility with legacy code)
    /// </summary>
    public void MarkAsProcessed()
    {
        ProcessingStatus = ValueObjects.ProcessingStatus.Completed();
        Statistics = Statistics.MarkAsProcessed();
        UpdateModificationInfo(null);
    }
}