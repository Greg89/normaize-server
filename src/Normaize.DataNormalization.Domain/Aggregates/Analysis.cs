using System;
using System.Collections.Generic;
using Normaize.DataNormalization.Domain.Events;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Domain.Aggregates;

/// <summary>
/// Represents an Analysis aggregate root
/// Manages the lifecycle of data analysis operations
/// </summary>
public class Analysis
{
    public AnalysisId Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public AnalysisType Type { get; private set; }
    public AnalysisStatus Status { get; private set; }
    public Guid DataSetId { get; private set; }
    public Guid? ComparisonDataSetId { get; private set; }
    public AnalysisConfiguration? Configuration { get; private set; }
    public AnalysisResult? Result { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Navigation properties
    public DataSet? DataSet { get; private set; }
    public DataSet? ComparisonDataSet { get; private set; }

    // Soft delete properties
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Analysis() // EF Core constructor
    {
        Id = AnalysisId.Unpersisted;
        Name = string.Empty;
    }

    private Analysis(
        string name,
        string? description,
        AnalysisType type,
        Guid dataSetId,
        Guid? comparisonDataSetId = null,
        AnalysisConfiguration? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Analysis name cannot be null or empty", nameof(name));

        if (name.Length > 255)
            throw new ArgumentException("Analysis name cannot exceed 255 characters", nameof(name));

        if (description?.Length > 1000)
            throw new ArgumentException("Analysis description cannot exceed 1000 characters", nameof(description));

        if (dataSetId == Guid.Empty)
            throw new ArgumentException("DataSet ID cannot be empty", nameof(dataSetId));

        // Validate that comparison dataset is different from primary dataset
        if (comparisonDataSetId.HasValue && comparisonDataSetId.Value == dataSetId)
            throw new ArgumentException("Comparison dataset cannot be the same as the primary dataset");

        Id = AnalysisId.Unpersisted; // Will be set by repository when persisted
        Name = name.Trim();
        Description = description?.Trim();
        Type = type;
        Status = AnalysisStatus.Pending;
        DataSetId = dataSetId;
        ComparisonDataSetId = comparisonDataSetId;
        Configuration = configuration;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a new analysis
    /// </summary>
    public static Analysis Create(
        string name,
        string? description,
        AnalysisType type,
        Guid dataSetId,
        Guid? comparisonDataSetId = null,
        AnalysisConfiguration? configuration = null)
    {
        var analysis = new Analysis(name, description, type, dataSetId, comparisonDataSetId, configuration);

        // Raise domain event for testing and consistency
        // Note: When persisting, SetId will ensure the event has the correct ID
        analysis._domainEvents.Add(new AnalysisCreated(analysis.Id, analysis.DataSetId, analysis.Type, analysis.Name));

        return analysis;
    }

    /// <summary>
    /// Starts the analysis execution
    /// </summary>
    public void Start()
    {
        if (Status != AnalysisStatus.Pending)
            throw new InvalidOperationException($"Cannot start analysis in {Status} status. Analysis must be in Pending status.");

        if (IsDeleted)
            throw new InvalidOperationException("Cannot start a deleted analysis");

        Status = AnalysisStatus.Processing;
        StartedAt = DateTime.UtcNow;
        ErrorMessage = null; // Clear any previous error

        _domainEvents.Add(new AnalysisStarted(Id, Type));
    }

    /// <summary>
    /// Completes the analysis with results
    /// </summary>
    public void Complete(AnalysisResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (Status != AnalysisStatus.Processing)
            throw new InvalidOperationException($"Cannot complete analysis in {Status} status. Analysis must be Processing.");

        if (IsDeleted)
            throw new InvalidOperationException("Cannot complete a deleted analysis");

        Status = AnalysisStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Result = result;
        ErrorMessage = null;

        _domainEvents.Add(new AnalysisCompleted(Id, result));
    }

    /// <summary>
    /// Marks the analysis as failed
    /// </summary>
    public void Fail(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

        if (Status != AnalysisStatus.Processing)
            throw new InvalidOperationException($"Cannot fail analysis in {Status} status. Analysis must be Processing.");

        if (IsDeleted)
            throw new InvalidOperationException("Cannot fail a deleted analysis");

        Status = AnalysisStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        Result = null; // Clear any partial results

        _domainEvents.Add(new AnalysisFailed(Id, errorMessage));
    }

    /// <summary>
    /// Resets the analysis to pending state for retry
    /// </summary>
    public void Reset()
    {
        if (Status == AnalysisStatus.Processing)
            throw new InvalidOperationException("Cannot reset analysis that is currently processing");

        if (IsDeleted)
            throw new InvalidOperationException("Cannot reset a deleted analysis");

        Status = AnalysisStatus.Pending;
        StartedAt = null;
        CompletedAt = null;
        ErrorMessage = null;
        Result = null;
    }

    /// <summary>
    /// Soft deletes the analysis
    /// </summary>
    public void Delete(string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));

        if (IsDeleted)
            throw new InvalidOperationException("Analysis is already deleted");

        if (Status == AnalysisStatus.Processing)
            throw new InvalidOperationException("Cannot delete analysis that is currently processing");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;

        _domainEvents.Add(new AnalysisDeleted(Id, deletedBy));
    }

    /// <summary>
    /// Restores a soft-deleted analysis
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("Analysis is not deleted");

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    /// <summary>
    /// Updates the analysis configuration
    /// </summary>
    public void UpdateConfiguration(AnalysisConfiguration configuration)
    {
        if (Status == AnalysisStatus.Processing)
            throw new InvalidOperationException("Cannot update configuration while analysis is processing");

        if (IsDeleted)
            throw new InvalidOperationException("Cannot update configuration of a deleted analysis");

        Configuration = configuration;
    }

    /// <summary>
    /// Updates the analysis name and description
    /// </summary>
    public void UpdateDetails(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Analysis name cannot be null or empty", nameof(name));

        if (name.Length > 255)
            throw new ArgumentException("Analysis name cannot exceed 255 characters", nameof(name));

        if (description?.Length > 1000)
            throw new ArgumentException("Analysis description cannot exceed 1000 characters", nameof(description));

        if (Status == AnalysisStatus.Processing)
            throw new InvalidOperationException("Cannot update details while analysis is processing");

        if (IsDeleted)
            throw new InvalidOperationException("Cannot update details of a deleted analysis");

        Name = name.Trim();
        Description = description?.Trim();
    }

    /// <summary>
    /// Sets the ID after persistence (called by repository)
    /// </summary>
    public void SetId(AnalysisId id)
    {
        if (Id.Value != 0)
            throw new InvalidOperationException("Analysis ID is already set");

        Id = id ?? throw new ArgumentNullException(nameof(id));

        // Clear any existing AnalysisCreated events and add a new one with the correct ID
        _domainEvents.RemoveAll(e => e is AnalysisCreated);
        _domainEvents.Add(new AnalysisCreated(Id, DataSetId, Type, Name));
    }

    /// <summary>
    /// Associates the DataSet entity with this analysis (for navigation purposes)
    /// </summary>
    public void SetDataSet(DataSet dataSet)
    {
        if (dataSet == null)
            throw new ArgumentNullException(nameof(dataSet));

        if (dataSet.Id != DataSetId)
            throw new ArgumentException($"DataSet ID {dataSet.Id} does not match analysis DataSet ID {DataSetId}");

        DataSet = dataSet;
    }

    /// <summary>
    /// Associates the comparison DataSet entity with this analysis (for navigation purposes)
    /// </summary>
    public void SetComparisonDataSet(DataSet comparisonDataSet)
    {
        if (comparisonDataSet == null)
            throw new ArgumentNullException(nameof(comparisonDataSet));

        if (!ComparisonDataSetId.HasValue)
            throw new InvalidOperationException("This analysis does not have a comparison dataset");

        if (comparisonDataSet.Id != ComparisonDataSetId.Value)
            throw new ArgumentException($"DataSet ID {comparisonDataSet.Id} does not match analysis comparison DataSet ID {ComparisonDataSetId}");

        ComparisonDataSet = comparisonDataSet;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Business rule queries
    public bool CanBeStarted => Status == AnalysisStatus.Pending && !IsDeleted;
    public bool CanBeReset => Status != AnalysisStatus.Processing && !IsDeleted;
    public bool CanBeDeleted => Status != AnalysisStatus.Processing && !IsDeleted;
    public bool IsRunning => Status == AnalysisStatus.Processing;
    public bool IsCompleted => Status == AnalysisStatus.Completed;
    public bool HasFailed => Status == AnalysisStatus.Failed;
    public bool HasResults => Result != null && Status == AnalysisStatus.Completed;
    public bool RequiresComparisonDataSet => Type == AnalysisType.Comparison;
    public TimeSpan? ExecutionDuration => StartedAt.HasValue && CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;
}