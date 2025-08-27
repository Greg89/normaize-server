using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Processor for removing duplicate rows from datasets
/// </summary>
public interface IDuplicateRowRemovalProcessor
{
    /// <summary>
    /// Processes a dataset to remove duplicate rows
    /// </summary>
    /// <param name="dataSet">Dataset to process</param>
    /// <param name="request">Duplicate removal configuration</param>
    /// <param name="progressCallback">Callback for progress updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results of the duplicate removal operation</returns>
    Task<NormalizationResults> ProcessAsync(
        DataSet dataSet,
        RemoveDuplicateRowsRequest request,
        IProgress<int> progressCallback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the time required to process a dataset
    /// </summary>
    /// <param name="dataSet">Dataset to estimate</param>
    /// <param name="request">Duplicate removal configuration</param>
    /// <returns>Estimated processing time in milliseconds</returns>
    Task<long> EstimateProcessingTimeAsync(DataSet dataSet, RemoveDuplicateRowsRequest request);

    /// <summary>
    /// Estimates the memory usage required to process a dataset
    /// </summary>
    /// <param name="dataSet">Dataset to estimate</param>
    /// <param name="request">Duplicate removal configuration</param>
    /// <returns>Estimated memory usage in MB</returns>
    Task<double> EstimateMemoryUsageAsync(DataSet dataSet, RemoveDuplicateRowsRequest request);

    /// <summary>
    /// Validates that the duplicate removal request can be processed
    /// </summary>
    /// <param name="dataSet">Dataset to validate</param>
    /// <param name="request">Duplicate removal configuration</param>
    /// <returns>Validation result</returns>
    Task<NormalizationValidationResult> ValidateRequestAsync(DataSet dataSet, RemoveDuplicateRowsRequest request);
}

/// <summary>
/// Result of a validation operation
/// </summary>
public class NormalizationValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static NormalizationValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static NormalizationValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a validation result with warnings
    /// </summary>
    public static NormalizationValidationResult SuccessWithWarnings(List<string> warnings) => new()
    {
        IsValid = true,
        Warnings = warnings
    };
}
