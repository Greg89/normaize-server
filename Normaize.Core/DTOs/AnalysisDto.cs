using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

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

/// <summary>
/// Data Transfer Object for comprehensive analysis information
/// </summary>
/// <remarks>
/// This DTO provides complete information about an analysis including metadata, status,
/// timing information, and results. It serves as the primary data transfer object for
/// analysis management operations and is used extensively by the DataAnalysisService
/// for CRUD operations, status tracking, and result retrieval.
/// </remarks>
public class AnalysisDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the analysis
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the analysis
    /// </summary>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the analysis
    /// </summary>
    [StringLength(1000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of analysis to be performed
    /// </summary>
    [JsonPropertyName("type")]
    public AnalysisType Type { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the analysis was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the analysis was completed (if applicable)
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the current status of the analysis
    /// </summary>
    [JsonPropertyName("status")]
    public AnalysisStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the analysis results in JSON format (if completed successfully)
    /// </summary>
    [JsonPropertyName("results")]
    public string? Results { get; set; }

    /// <summary>
    /// Gets or sets the error message if the analysis failed
    /// </summary>
    [StringLength(2000)]
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the primary dataset for analysis
    /// </summary>
    [JsonPropertyName("dataSetId")]
    public int DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the comparison dataset (if applicable)
    /// </summary>
    [JsonPropertyName("comparisonDataSetId")]
    public int? ComparisonDataSetId { get; set; }
}

/// <summary>
/// Data Transfer Object for creating new analysis operations
/// </summary>
/// <remarks>
/// This DTO is used to initiate new analysis operations. It contains the essential
/// information required to create an analysis including the analysis type, target
/// dataset, and optional configuration parameters. Used by the DataAnalysisService
/// for analysis creation and validation.
/// </remarks>
public class CreateAnalysisDto
{
    /// <summary>
    /// Gets or sets the name of the analysis
    /// </summary>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the analysis
    /// </summary>
    [StringLength(1000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of analysis to be performed
    /// </summary>
    [JsonPropertyName("type")]
    public AnalysisType Type { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the primary dataset for analysis
    /// </summary>
    [JsonPropertyName("dataSetId")]
    public int DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the comparison dataset (if applicable)
    /// </summary>
    [JsonPropertyName("comparisonDataSetId")]
    public int? ComparisonDataSetId { get; set; }

    /// <summary>
    /// Gets or sets the optional configuration parameters in JSON format
    /// </summary>
    [StringLength(5000)]
    [JsonPropertyName("configuration")]
    public string? Configuration { get; set; }
}

/// <summary>
/// Data Transfer Object for analysis results and status information
/// </summary>
/// <remarks>
/// This DTO provides the results of an analysis operation along with status information.
/// It is used to return analysis results to clients and contains either the analysis
/// results or error information depending on the analysis status.
/// </remarks>
public class AnalysisResultDto
{
    /// <summary>
    /// Gets or sets the identifier of the analysis
    /// </summary>
    [JsonPropertyName("analysisId")]
    public int AnalysisId { get; set; }

    /// <summary>
    /// Gets or sets the current status of the analysis
    /// </summary>
    [JsonPropertyName("status")]
    public AnalysisStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the analysis results as a dynamic object (if completed successfully)
    /// </summary>
    [JsonPropertyName("results")]
    public object? Results { get; set; }

    /// <summary>
    /// Gets or sets the error message if the analysis failed
    /// </summary>
    [StringLength(2000)]
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}