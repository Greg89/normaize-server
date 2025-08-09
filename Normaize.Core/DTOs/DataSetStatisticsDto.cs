using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Data Transfer Object for dataset statistics and summary information
/// </summary>
/// <remarks>
/// This DTO provides aggregated statistics about a user's datasets including total count,
/// total size, and recently modified datasets. It serves as the data transfer object for
/// dataset statistics endpoints and supports dataset analytics and dashboard functionality.
/// 
/// The statistics are typically cached for performance optimization and include both
/// quantitative metrics (count, size) and qualitative data (recently modified datasets)
/// to provide comprehensive insights into the user's dataset collection.
/// </remarks>
public class DataSetStatisticsDto
{
    /// <summary>
    /// Gets or sets the total number of datasets owned by the user
    /// </summary>
    /// <remarks>
    /// Represents the complete count of datasets in the user's collection.
    /// This includes all datasets regardless of their processing status or file type.
    /// Used for dashboard displays and analytics reporting.
    /// </remarks>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the total size of all datasets in bytes
    /// </summary>
    /// <remarks>
    /// Represents the combined file size of all datasets in the user's collection.
    /// This metric helps users understand their storage usage and can be used
    /// for storage quota management and cost analysis.
    /// </remarks>
    [JsonPropertyName("totalSize")]
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the collection of recently modified datasets
    /// </summary>
    /// <remarks>
    /// Contains a list of datasets that have been recently modified or updated.
    /// Typically limited to the most recent 5-10 datasets for performance reasons.
    /// Used for displaying recent activity and providing quick access to recently
    /// worked-on datasets in the user interface.
    /// </remarks>
    [JsonPropertyName("recentlyModified")]
    public IEnumerable<DataSetDto> RecentlyModified { get; set; } = new List<DataSetDto>();

    /// <summary>
    /// Gets or sets the collection of recently uploaded datasets
    /// </summary>
    /// <remarks>
    /// Contains a list of datasets that have been recently uploaded.
    /// Typically limited to the most recent 5-10 uploads for performance reasons.
    /// Used for displaying recent upload activity and providing quick access to recently
    /// uploaded datasets in the user interface.
    /// </remarks>
    [JsonPropertyName("recentUploads")]
    public IEnumerable<DataSetDto> RecentUploads { get; set; } = new List<DataSetDto>();

    /// <summary>
    /// Gets or sets the total number of datasets (including deleted ones)
    /// </summary>
    [JsonPropertyName("totalDataSets")]
    public int TotalDataSets { get; set; }

    /// <summary>
    /// Gets or sets the number of deleted datasets
    /// </summary>
    [JsonPropertyName("deletedDataSets")]
    public int DeletedDataSets { get; set; }

    /// <summary>
    /// Gets or sets the total file size in bytes
    /// </summary>
    [JsonPropertyName("totalFileSize")]
    public long TotalFileSize { get; set; }

    /// <summary>
    /// Gets or sets the average file size in bytes
    /// </summary>
    [JsonPropertyName("averageFileSize")]
    public long AverageFileSize { get; set; }

    /// <summary>
    /// Gets or sets the breakdown of datasets by file type
    /// </summary>
    [JsonPropertyName("fileTypeBreakdown")]
    public Dictionary<string, int> FileTypeBreakdown { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets or sets the breakdown of datasets by processing status
    /// </summary>
    [JsonPropertyName("processingStatusBreakdown")]
    public Dictionary<string, int> ProcessingStatusBreakdown { get; set; } = new Dictionary<string, int>();
}