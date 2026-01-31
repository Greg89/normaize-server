using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.DTOs;

/// <summary>
/// Data Transfer Object for comprehensive analysis information
/// </summary>
public class AnalysisDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the analysis
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

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
    /// Gets or sets the timestamp when the analysis was started (if applicable)
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }

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
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the comparison dataset (if applicable)
    /// </summary>
    [JsonPropertyName("comparisonDataSetId")]
    public Guid? ComparisonDataSetId { get; set; }

    /// <summary>
    /// Gets or sets the configuration parameters in JSON format (if applicable)
    /// </summary>
    [JsonPropertyName("configuration")]
    public string? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds (if applicable)
    /// </summary>
    [JsonPropertyName("executionDurationMs")]
    public long? ExecutionDurationMs { get; set; }

    /// <summary>
    /// Gets or sets whether the analysis is deleted
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Data Transfer Object for creating new analysis operations
/// </summary>
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
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the comparison dataset (if applicable)
    /// </summary>
    [JsonPropertyName("comparisonDataSetId")]
    public Guid? ComparisonDataSetId { get; set; }

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
public class AnalysisResultDto
{
    /// <summary>
    /// Gets or sets the identifier of the analysis
    /// </summary>
    [JsonPropertyName("analysisId")]
    public Guid AnalysisId { get; set; }

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

    /// <summary>
    /// Gets or sets the execution duration in milliseconds (if applicable)
    /// </summary>
    [JsonPropertyName("executionDurationMs")]
    public long? ExecutionDurationMs { get; set; }
}