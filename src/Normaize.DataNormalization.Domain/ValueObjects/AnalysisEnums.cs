namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Represents the current status of an analysis operation
/// </summary>
/// <remarks>
/// This enum defines the various states an analysis can be in during its lifecycle,
/// from initial creation through completion or failure. Used throughout the analysis
/// workflow to track progress and determine appropriate actions.
/// </remarks>
public enum AnalysisStatus
{
    /// <summary>Analysis has been created but not yet started</summary>
    Pending,
    /// <summary>Analysis is currently being executed</summary>
    Processing,
    /// <summary>Analysis has completed successfully</summary>
    Completed,
    /// <summary>Analysis has failed during execution</summary>
    Failed
}

/// <summary>
/// Defines the types of analysis operations supported by the platform
/// </summary>
/// <remarks>
/// This enum represents the different categories of data analysis that can be performed
/// on datasets. Each type corresponds to specific algorithms and processing methods
/// implemented in the DataAnalysisService.
/// </remarks>
public enum AnalysisType
{
    /// <summary>Data normalization analysis</summary>
    Normalization,
    /// <summary>Dataset comparison analysis</summary>
    Comparison,
    /// <summary>Statistical analysis and metrics</summary>
    Statistical,
    /// <summary>Data cleaning and preprocessing</summary>
    DataCleaning,
    /// <summary>Outlier detection and analysis</summary>
    OutlierDetection,
    /// <summary>Correlation analysis between variables</summary>
    CorrelationAnalysis,
    /// <summary>Trend analysis and time series</summary>
    TrendAnalysis,
    /// <summary>Custom analysis with user-defined parameters</summary>
    Custom
}