using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Domain.Events;

namespace Normaize.DataNormalization.Domain.Aggregates;

/// <summary>
/// Aggregate root for managing statistical calculations and data summaries
/// </summary>
public class Statistics
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public StatisticsId Id { get; private set; }
    public Guid DataSetId { get; private set; }
    public string DataSetName { get; private set; }
    public int TotalRows { get; private set; }
    public int TotalColumns { get; private set; }
    public int MissingValues { get; private set; }
    public int DuplicateRows { get; private set; }
    public IReadOnlyDictionary<string, ColumnSummary> ColumnSummaries { get; private set; }
    public IReadOnlyDictionary<string, StatisticalMeasure> ColumnStatistics { get; private set; }
    public DateTime CalculatedAt { get; private set; }
    public TimeSpan ProcessingTime { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Statistics()
    {
        Id = StatisticsId.Unpersisted;
        DataSetName = string.Empty;
        ColumnSummaries = new Dictionary<string, ColumnSummary>();
        ColumnStatistics = new Dictionary<string, StatisticalMeasure>();
    }

    private Statistics(
        Guid dataSetId,
        string dataSetName,
        int totalRows,
        int totalColumns,
        int missingValues,
        int duplicateRows,
        Dictionary<string, ColumnSummary> columnSummaries,
        Dictionary<string, StatisticalMeasure> columnStatistics,
        TimeSpan processingTime)
    {
        if (dataSetId == Guid.Empty)
            throw new ArgumentException("DataSet ID cannot be empty", nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(dataSetName))
            throw new ArgumentException("DataSet name cannot be null or empty", nameof(dataSetName));
        
        if (totalRows < 0)
            throw new ArgumentException("Total rows cannot be negative", nameof(totalRows));
        
        if (totalColumns < 0)
            throw new ArgumentException("Total columns cannot be negative", nameof(totalColumns));
        
        if (missingValues < 0)
            throw new ArgumentException("Missing values cannot be negative", nameof(missingValues));
        
        if (duplicateRows < 0)
            throw new ArgumentException("Duplicate rows cannot be negative", nameof(duplicateRows));

        Id = StatisticsId.Unpersisted;
        DataSetId = dataSetId;
        DataSetName = dataSetName;
        TotalRows = totalRows;
        TotalColumns = totalColumns;
        MissingValues = missingValues;
        DuplicateRows = duplicateRows;
        ColumnSummaries = columnSummaries.AsReadOnly();
        ColumnStatistics = columnStatistics.AsReadOnly();
        CalculatedAt = DateTime.UtcNow;
        ProcessingTime = processingTime;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates basic data summary statistics
    /// </summary>
    public static Statistics CreateDataSummary(
        Guid dataSetId,
        string dataSetName,
        int totalRows,
        int totalColumns,
        int missingValues,
        int duplicateRows,
        Dictionary<string, ColumnSummary> columnSummaries,
        TimeSpan processingTime)
    {
        var statistics = new Statistics(
            dataSetId,
            dataSetName,
            totalRows,
            totalColumns,
            missingValues,
            duplicateRows,
            columnSummaries,
            new Dictionary<string, StatisticalMeasure>(),
            processingTime);

        statistics._domainEvents.Add(new DataSummaryCalculated(
            statistics.Id,
            dataSetId,
            totalRows,
            totalColumns,
            missingValues,
            duplicateRows,
            columnSummaries.Count));

        return statistics;
    }

    /// <summary>
    /// Creates comprehensive statistical summary
    /// </summary>
    public static Statistics CreateStatisticalSummary(
        Guid dataSetId,
        string dataSetName,
        int totalRows,
        int totalColumns,
        int missingValues,
        int duplicateRows,
        Dictionary<string, ColumnSummary> columnSummaries,
        Dictionary<string, StatisticalMeasure> columnStatistics,
        TimeSpan processingTime)
    {
        var statistics = new Statistics(
            dataSetId,
            dataSetName,
            totalRows,
            totalColumns,
            missingValues,
            duplicateRows,
            columnSummaries,
            columnStatistics,
            processingTime);

        statistics._domainEvents.Add(new StatisticalSummaryCalculated(
            statistics.Id,
            dataSetId,
            columnStatistics.Count,
            columnStatistics.Values.Count(s => s.IsSignificantlySkewed),
            columnStatistics.Values.Count(s => s.IsHighKurtosis)));

        return statistics;
    }

    /// <summary>
    /// Updates the statistics with new calculations
    /// </summary>
    public void UpdateCalculations(
        Dictionary<string, ColumnSummary> newColumnSummaries,
        Dictionary<string, StatisticalMeasure> newColumnStatistics,
        TimeSpan newProcessingTime)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update deleted statistics");

        ColumnSummaries = newColumnSummaries.AsReadOnly();
        ColumnStatistics = newColumnStatistics.AsReadOnly();
        ProcessingTime = newProcessingTime;
        CalculatedAt = DateTime.UtcNow;

        _domainEvents.Add(new StatisticsUpdated(Id, DataSetId, CalculatedAt));
    }

    /// <summary>
    /// Marks the statistics as deleted
    /// </summary>
    public void Delete(string deletedBy)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Statistics are already deleted");

        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("Deleted by cannot be null or empty", nameof(deletedBy));

        IsDeleted = true;
        _domainEvents.Add(new StatisticsDeleted(Id, DataSetId, deletedBy));
    }

    /// <summary>
    /// Sets the ID when the entity is persisted (used by repository)
    /// </summary>
    public void SetId(StatisticsId? id)
    {
        if (Id.Value != 0)
            throw new InvalidOperationException("Statistics ID is already set");

        Id = id ?? throw new ArgumentNullException(nameof(id));
        
        // Update any existing domain events with the correct ID
        for (int i = 0; i < _domainEvents.Count; i++)
        {
            var domainEvent = _domainEvents[i];
            if (domainEvent is DataSummaryCalculated dataSummary && dataSummary.StatisticsId.Value == 0)
            {
                _domainEvents[i] = new DataSummaryCalculated(
                    Id,
                    dataSummary.DataSetId,
                    dataSummary.TotalRows,
                    dataSummary.TotalColumns,
                    dataSummary.MissingValues,
                    dataSummary.DuplicateRows,
                    dataSummary.ColumnCount);
            }
            else if (domainEvent is StatisticalSummaryCalculated statSummary && statSummary.StatisticsId.Value == 0)
            {
                _domainEvents[i] = new StatisticalSummaryCalculated(
                    Id,
                    statSummary.DataSetId,
                    statSummary.StatisticalColumnCount,
                    statSummary.SkewedColumnCount,
                    statSummary.HighKurtosisColumnCount);
            }
        }
    }

    /// <summary>
    /// Gets statistics for numeric columns only
    /// </summary>
    public IReadOnlyDictionary<string, StatisticalMeasure> GetNumericColumnStatistics()
    {
        return ColumnStatistics
            .Where(kvp => ColumnSummaries.ContainsKey(kvp.Key) && 
                         ColumnSummaries[kvp.Key].DataType.IsNumeric)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            .AsReadOnly();
    }

    /// <summary>
    /// Gets summary of data quality issues
    /// </summary>
    public DataQualitySummary GetDataQualitySummary()
    {
        var highNullColumns = ColumnSummaries.Values.Count(c => c.HasHighNullRate);
        var highCardinalityColumns = ColumnSummaries.Values.Count(c => c.IsHighCardinality);
        var missingDataPercentage = TotalRows > 0 ? (double)MissingValues / (TotalRows * TotalColumns) * 100 : 0;
        var duplicatePercentage = TotalRows > 0 ? (double)DuplicateRows / TotalRows * 100 : 0;

        return new DataQualitySummary(
            highNullColumns,
            highCardinalityColumns,
            missingDataPercentage,
            duplicatePercentage,
            DuplicateRows > 0 || MissingValues > 0);
    }

    /// <summary>
    /// Marks the statistics as deleted (soft delete)
    /// </summary>
    public void SoftDelete(string deletedBy = "System")
    {
        IsDeleted = true;
        
        // Add domain event
        _domainEvents.Add(new StatisticsDeleted(Id, DataSetId, deletedBy));
    }

    /// <summary>
    /// Updates the statistics with new data
    /// </summary>
    public void Update(
        int totalRows,
        int totalColumns,
        int missingValues,
        int duplicateRows,
        Dictionary<string, ColumnSummary> columnSummaries,
        Dictionary<string, StatisticalMeasure> columnStatistics,
        TimeSpan processingTime)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update deleted statistics");
            
        if (totalRows < 0)
            throw new ArgumentException("Total rows cannot be negative", nameof(totalRows));
        
        if (totalColumns < 0)
            throw new ArgumentException("Total columns cannot be negative", nameof(totalColumns));
        
        if (missingValues < 0)
            throw new ArgumentException("Missing values cannot be negative", nameof(missingValues));
        
        if (duplicateRows < 0)
            throw new ArgumentException("Duplicate rows cannot be negative", nameof(duplicateRows));

        var updatedAt = DateTime.UtcNow;
        TotalRows = totalRows;
        TotalColumns = totalColumns;
        MissingValues = missingValues;
        DuplicateRows = duplicateRows;
        ColumnSummaries = columnSummaries.AsReadOnly();
        ColumnStatistics = columnStatistics.AsReadOnly();
        ProcessingTime = processingTime;
        CalculatedAt = updatedAt;
        
        // Add domain event
        _domainEvents.Add(new StatisticsUpdated(Id, DataSetId, updatedAt));
    }

    /// <summary>
    /// Clears all domain events
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}