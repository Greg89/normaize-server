using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing summary statistics for a single column
/// </summary>
public record ColumnSummary
{
    public string ColumnName { get; init; }
    public DataTypeClassification DataType { get; init; }
    public int NonNullCount { get; init; }
    public int NullCount { get; init; }
    public int UniqueCount { get; init; }
    public IReadOnlyList<object> SampleValues { get; init; }
    public object? MinValue { get; init; }
    public object? MaxValue { get; init; }
    public StatisticalMeasure? Statistics { get; init; }

    public ColumnSummary(
        string columnName,
        DataTypeClassification dataType,
        int nonNullCount,
        int nullCount,
        int uniqueCount,
        IEnumerable<object> sampleValues,
        object? minValue = null,
        object? maxValue = null,
        StatisticalMeasure? statistics = null)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

        if (nonNullCount < 0)
            throw new ArgumentException("Non-null count cannot be negative", nameof(nonNullCount));

        if (nullCount < 0)
            throw new ArgumentException("Null count cannot be negative", nameof(nullCount));

        if (uniqueCount < 0)
            throw new ArgumentException("Unique count cannot be negative", nameof(uniqueCount));

        ArgumentNullException.ThrowIfNull(dataType);

        ColumnName = columnName;
        DataType = dataType;
        NonNullCount = nonNullCount;
        NullCount = nullCount;
        UniqueCount = uniqueCount;
        SampleValues = sampleValues.Take(10).ToList().AsReadOnly(); // Limit sample values
        MinValue = minValue;
        MaxValue = maxValue;
        Statistics = statistics;
    }

    /// <summary>
    /// Gets the total count of values (null + non-null)
    /// </summary>
    public int TotalCount => NonNullCount + NullCount;

    /// <summary>
    /// Gets the percentage of null values
    /// </summary>
    public double NullPercentage => TotalCount > 0 ? (double)NullCount / TotalCount * 100 : 0;

    /// <summary>
    /// Gets the percentage of unique values
    /// </summary>
    public double UniquenessPercentage => NonNullCount > 0 ? (double)UniqueCount / NonNullCount * 100 : 0;

    /// <summary>
    /// Indicates if this column has statistical measures available
    /// </summary>
    public bool HasStatistics => Statistics != null && DataType.CanCalculateStatistics;

    /// <summary>
    /// Indicates if this column has high cardinality (many unique values)
    /// </summary>
    public bool IsHighCardinality => UniquenessPercentage > 90;

    /// <summary>
    /// Indicates if this column has many null values
    /// </summary>
    public bool HasHighNullRate => NullPercentage > 25;
}