namespace Normaize.DataNormalization.Application.DTOs;

/// <summary>
/// Complete statistics information DTO
/// </summary>
public class StatisticsDto
{
    /// <summary>
    /// Statistics identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Dataset identifier
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Dataset name
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of rows in the dataset
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Total number of columns in the dataset
    /// </summary>
    public int TotalColumns { get; set; }

    /// <summary>
    /// Total number of missing values across all columns
    /// </summary>
    public int MissingValues { get; set; }

    /// <summary>
    /// Number of duplicate rows identified
    /// </summary>
    public int DuplicateRows { get; set; }

    /// <summary>
    /// When the statistics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Time taken to process the statistics
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Column-level summaries
    /// </summary>
    public Dictionary<string, DetailedColumnSummaryDto> ColumnSummaries { get; set; } = new();

    /// <summary>
    /// Statistical measures for numeric columns
    /// </summary>
    public Dictionary<string, StatisticalMeasureDto> ColumnStatistics { get; set; } = new();
}

/// <summary>
/// Detailed column summary information DTO
/// </summary>
public class DetailedColumnSummaryDto
{
    /// <summary>
    /// Column name
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Data type classification
    /// </summary>
    public DataTypeClassificationDto DataType { get; set; } = new();

    /// <summary>
    /// Count of non-null values
    /// </summary>
    public int NonNullCount { get; set; }

    /// <summary>
    /// Count of null values
    /// </summary>
    public int NullCount { get; set; }

    /// <summary>
    /// Count of unique values
    /// </summary>
    public int UniqueCount { get; set; }

    /// <summary>
    /// Sample values from the column
    /// </summary>
    public List<object> SampleValues { get; set; } = new();

    /// <summary>
    /// Minimum value (if applicable)
    /// </summary>
    public object? MinValue { get; set; }

    /// <summary>
    /// Maximum value (if applicable)
    /// </summary>
    public object? MaxValue { get; set; }

    /// <summary>
    /// Statistical measures (for numeric columns)
    /// </summary>
    public StatisticalMeasureDto? Statistics { get; set; }
}

/// <summary>
/// Data type classification DTO
/// </summary>
public class DataTypeClassificationDto
{
    /// <summary>
    /// Name of the data type
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the type is numeric
    /// </summary>
    public bool IsNumeric { get; set; }

    /// <summary>
    /// Whether the type is date/time
    /// </summary>
    public bool IsDateTime { get; set; }

    /// <summary>
    /// Whether the type is boolean
    /// </summary>
    public bool IsBoolean { get; set; }
}

/// <summary>
/// Statistical measure DTO
/// </summary>
public class StatisticalMeasureDto
{
    /// <summary>
    /// Mean value
    /// </summary>
    public double Mean { get; set; }

    /// <summary>
    /// Median value
    /// </summary>
    public double Median { get; set; }

    /// <summary>
    /// Standard deviation
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Minimum value
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Maximum value
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// First quartile (25th percentile)
    /// </summary>
    public double Q1 { get; set; }

    /// <summary>
    /// Second quartile (50th percentile, same as median)
    /// </summary>
    public double Q2 { get; set; }

    /// <summary>
    /// Third quartile (75th percentile)
    /// </summary>
    public double Q3 { get; set; }

    /// <summary>
    /// Skewness measure
    /// </summary>
    public double Skewness { get; set; }

    /// <summary>
    /// Kurtosis measure
    /// </summary>
    public double Kurtosis { get; set; }

    /// <summary>
    /// Number of outliers detected
    /// </summary>
    public int OutlierCount { get; set; }
}