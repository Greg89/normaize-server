namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing a summary of data quality issues
/// </summary>
public record DataQualitySummary
{
    public int ColumnsWithHighNullRate { get; init; }
    public int HighCardinalityColumns { get; init; }
    public double MissingDataPercentage { get; init; }
    public double DuplicateRowsPercentage { get; init; }
    public bool HasQualityIssues { get; init; }

    public DataQualitySummary(
        int columnsWithHighNullRate,
        int highCardinalityColumns,
        double missingDataPercentage,
        double duplicateRowsPercentage,
        bool hasQualityIssues)
    {
        if (columnsWithHighNullRate < 0)
            throw new ArgumentException("Columns with high null rate cannot be negative", nameof(columnsWithHighNullRate));

        if (highCardinalityColumns < 0)
            throw new ArgumentException("High cardinality columns cannot be negative", nameof(highCardinalityColumns));

        if (missingDataPercentage < 0 || missingDataPercentage > 100)
            throw new ArgumentException("Missing data percentage must be between 0 and 100", nameof(missingDataPercentage));

        if (duplicateRowsPercentage < 0 || duplicateRowsPercentage > 100)
            throw new ArgumentException("Duplicate rows percentage must be between 0 and 100", nameof(duplicateRowsPercentage));

        ColumnsWithHighNullRate = columnsWithHighNullRate;
        HighCardinalityColumns = highCardinalityColumns;
        MissingDataPercentage = missingDataPercentage;
        DuplicateRowsPercentage = duplicateRowsPercentage;
        HasQualityIssues = hasQualityIssues;
    }

    /// <summary>
    /// Creates a summary indicating no quality issues
    /// </summary>
    public static DataQualitySummary NoIssues => new(0, 0, 0, 0, false);

    /// <summary>
    /// Indicates if the data has serious quality issues (>50% missing data or >25% duplicates)
    /// </summary>
    public bool HasSeriousIssues => MissingDataPercentage > 50 || DuplicateRowsPercentage > 25;

    /// <summary>
    /// Gets the overall data quality score (0-100, higher is better)
    /// </summary>
    public int QualityScore
    {
        get
        {
            var score = 100.0;

            // Deduct points for missing data
            score -= MissingDataPercentage * 0.5;

            // Deduct points for duplicates
            score -= DuplicateRowsPercentage * 0.3;

            // Deduct points for high null rate columns
            score -= ColumnsWithHighNullRate * 5;

            // Deduct points for high cardinality columns
            score -= HighCardinalityColumns * 2;

            return Math.Max(0, (int)Math.Round(score));
        }
    }
}