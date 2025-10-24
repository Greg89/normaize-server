using System;
using System.Collections.Generic;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Domain.Events;

namespace Normaize.DataNormalization.Domain.Entities;

/// <summary>
/// DataSet entity for the Data Normalization bounded context
/// This is NOT an aggregate root - it's referenced by NormalizationJob aggregate
/// </summary>
public class DataSet
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string UserId { get; private set; }
    public FileMetadata FileInfo { get; private set; }
    public DatasetStatistics Statistics { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public string? Schema { get; private set; }
    public string? PreviewData { get; private set; }
    public string? ProcessedData { get; private set; }
    public string? ProcessingErrors { get; private set; }
    public DateTime? RetentionExpiryDate { get; private set; }
    
    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }
    
    // Audit trail
    public DateTime LastModifiedAt { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private DataSet() // EF Core constructor
    {
        Name = string.Empty;
        UserId = string.Empty;
        FileInfo = null!;
        Statistics = null!;
    }

    private DataSet(
        string name,
        string? description,
        string userId,
        FileMetadata fileInfo,
        DatasetStatistics statistics)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        UploadedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a new dataset
    /// </summary>
    public static DataSet Create(
        string name,
        string? description,
        string userId,
        FileMetadata fileInfo,
        DatasetStatistics? statistics = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Dataset name cannot be null or empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var datasetStats = statistics ?? DatasetStatistics.Empty;
        
        return new DataSet(name.Trim(), description?.Trim(), userId.Trim(), fileInfo, datasetStats);
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
        
        RetentionExpiryDate = expiryDate;
        UpdateModificationInfo(modifiedBy);
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
    }

    // Business rule queries
    public bool IsRetentionExpired => RetentionExpiryDate.HasValue && RetentionExpiryDate.Value <= DateTime.UtcNow;
    public bool IsProcessed => Statistics.IsProcessed;
    public bool IsLargeDataset => Statistics.IsLargeDataset;
    public bool RequiresSeparateTable => Statistics.UseSeparateTable;
    public bool HasProcessingErrors => !string.IsNullOrWhiteSpace(ProcessingErrors);
    public bool IsTextBasedFile => FileInfo.FileType.IsTextBased;
    public bool IsStoredInCloud => FileInfo.StorageProvider.IsCloudBased;

    private void UpdateModificationInfo(string? modifiedBy)
    {
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}