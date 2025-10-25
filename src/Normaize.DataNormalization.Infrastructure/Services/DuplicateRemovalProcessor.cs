using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Service for performing duplicate removal operations on dataset data
/// </summary>
public interface IDuplicateRemovalProcessor
{
    /// <summary>
    /// Removes duplicates from dataset data based on specified options
    /// </summary>
    /// <param name="data">The dataset data to process</param>
    /// <param name="options">Duplicate removal configuration</param>
    /// <param name="progressCallback">Progress reporting callback</param>
    /// <returns>Processed data with duplicates removed</returns>
    Task<DuplicateRemovalResult> RemoveDuplicatesAsync(
        DataSetData data,
        DuplicateRemovalOptions options,
        IProgress<DuplicateRemovalProgress> progressCallback);
}

/// <summary>
/// Result of duplicate removal operation
/// </summary>
public class DuplicateRemovalResult
{
    public DataSetData ProcessedData { get; init; }
    public int OriginalRowCount { get; init; }
    public int DuplicatesRemoved { get; init; }
    public int FinalRowCount { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public IReadOnlyList<string> ProcessedColumns { get; init; }
    public CaseSensitivity CaseSensitivity { get; init; }
    public RetentionStrategy RetentionStrategy { get; init; }

    public DuplicateRemovalResult(
        DataSetData processedData,
        int originalRowCount,
        int duplicatesRemoved,
        TimeSpan processingTime,
        IReadOnlyList<string> processedColumns,
        CaseSensitivity caseSensitivity,
        RetentionStrategy retentionStrategy)
    {
        ProcessedData = processedData;
        OriginalRowCount = originalRowCount;
        DuplicatesRemoved = duplicatesRemoved;
        FinalRowCount = processedData.TotalRows;
        ProcessingTime = processingTime;
        ProcessedColumns = processedColumns;
        CaseSensitivity = caseSensitivity;
        RetentionStrategy = retentionStrategy;
    }
}

/// <summary>
/// Progress information for duplicate removal operation
/// </summary>
public class DuplicateRemovalProgress
{
    public int PercentComplete { get; init; }
    public string CurrentOperation { get; init; } = string.Empty;
    public int RowsProcessed { get; init; }
    public int TotalRows { get; init; }
    public int DuplicatesFoundSoFar { get; init; }

    public DuplicateRemovalProgress(int percentComplete, string currentOperation, int rowsProcessed, int totalRows, int duplicatesFoundSoFar = 0)
    {
        PercentComplete = percentComplete;
        CurrentOperation = currentOperation;
        RowsProcessed = rowsProcessed;
        TotalRows = totalRows;
        DuplicatesFoundSoFar = duplicatesFoundSoFar;
    }
}

/// <summary>
/// Implementation of duplicate removal processor
/// </summary>
public class DuplicateRemovalProcessor : IDuplicateRemovalProcessor
{
    private readonly ILogger<DuplicateRemovalProcessor> _logger;

    public DuplicateRemovalProcessor(ILogger<DuplicateRemovalProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DuplicateRemovalResult> RemoveDuplicatesAsync(
        DataSetData data,
        DuplicateRemovalOptions options,
        IProgress<DuplicateRemovalProgress> progressCallback)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting duplicate removal for {RowCount} rows using columns: {KeyColumns}",
            data.TotalRows, string.Join(", ", options.KeyColumns));

        try
        {
            // Step 1: Validate key columns exist
            progressCallback?.Report(new DuplicateRemovalProgress(5, "Validating key columns", 0, data.TotalRows));
            ValidateKeyColumns(data, options);

            // Step 2: Create duplicate detection keys
            progressCallback?.Report(new DuplicateRemovalProgress(10, "Analyzing data structure", 0, data.TotalRows));
            var duplicateGroups = await CreateDuplicateGroupsAsync(data, options, progressCallback);

            // Step 3: Apply retention strategy
            progressCallback?.Report(new DuplicateRemovalProgress(60, "Applying retention strategy", 0, data.TotalRows));
            var retainedRows = ApplyRetentionStrategy(duplicateGroups, options, progressCallback);

            // Step 4: Create processed dataset
            progressCallback?.Report(new DuplicateRemovalProgress(90, "Creating processed dataset", 0, data.TotalRows));
            var processedData = CreateProcessedDataSet(data, retainedRows, options);

            var processingTime = DateTime.UtcNow - startTime;
            var duplicatesRemoved = data.TotalRows - processedData.TotalRows;

            progressCallback?.Report(new DuplicateRemovalProgress(100, "Duplicate removal completed", processedData.TotalRows, data.TotalRows, duplicatesRemoved));

            _logger.LogInformation("Duplicate removal completed. Original: {OriginalCount}, Final: {FinalCount}, Removed: {RemovedCount}, Time: {ProcessingTime}ms",
                data.TotalRows, processedData.TotalRows, duplicatesRemoved, processingTime.TotalMilliseconds);

            return new DuplicateRemovalResult(
                processedData,
                data.TotalRows,
                duplicatesRemoved,
                processingTime,
                options.KeyColumns,
                options.CaseSensitivity,
                options.RetentionStrategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove duplicates");
            throw;
        }
    }

    private static void ValidateKeyColumns(DataSetData data, DuplicateRemovalOptions options)
    {
        var availableColumns = data.Columns.Select(c => c.Name).ToHashSet();
        var missingColumns = options.KeyColumns.Where(col => !availableColumns.Contains(col)).ToList();

        if (missingColumns.Any())
        {
            throw new ArgumentException($"Key columns not found in dataset: {string.Join(", ", missingColumns)}");
        }
    }

    private async Task<Dictionary<string, List<DataSetRowData>>> CreateDuplicateGroupsAsync(
        DataSetData data,
        DuplicateRemovalOptions options,
        IProgress<DuplicateRemovalProgress> progressCallback)
    {
        var duplicateGroups = new Dictionary<string, List<DataSetRowData>>();
        var processedRows = 0;
        var totalRows = data.TotalRows;

        foreach (var row in data.Rows)
        {
            // Create a composite key from the specified columns
            var key = CreateCompositeKey(row, options);

            if (!duplicateGroups.ContainsKey(key))
            {
                duplicateGroups[key] = new List<DataSetRowData>();
            }

            duplicateGroups[key].Add(row);

            processedRows++;

            // Report progress every 1000 rows or at the end
            if (processedRows % 1000 == 0 || processedRows == totalRows)
            {
                var percentComplete = 10 + (int)((double)processedRows / totalRows * 50); // 10-60%
                progressCallback?.Report(new DuplicateRemovalProgress(
                    percentComplete,
                    "Identifying duplicate groups",
                    processedRows,
                    totalRows));

                // Yield control to prevent blocking
                await Task.Yield();
            }
        }

        _logger.LogInformation("Created {GroupCount} unique groups from {RowCount} rows. Duplicate groups: {DuplicateGroupCount}",
            duplicateGroups.Count, totalRows, duplicateGroups.Count(g => g.Value.Count > 1));

        return duplicateGroups;
    }

    private string CreateCompositeKey(DataSetRowData row, DuplicateRemovalOptions options)
    {
        var keyParts = new List<string>();

        foreach (var columnName in options.KeyColumns)
        {
            var value = row.GetValue<string>(columnName) ?? string.Empty;

            // Apply case sensitivity
            if (options.CaseSensitivity == CaseSensitivity.Insensitive)
            {
                value = value.ToLowerInvariant();
            }

            keyParts.Add(value);
        }

        return string.Join("||", keyParts); // Use || as separator to avoid conflicts
    }

    private List<DataSetRowData> ApplyRetentionStrategy(
        Dictionary<string, List<DataSetRowData>> duplicateGroups,
        DuplicateRemovalOptions options,
        IProgress<DuplicateRemovalProgress> progressCallback)
    {
        var retainedRows = new List<DataSetRowData>();
        var processedGroups = 0;
        var totalGroups = duplicateGroups.Count;

        foreach (var group in duplicateGroups.Values)
        {
            DataSetRowData rowToRetain;

            if (group.Count == 1)
            {
                // No duplicates, keep the only row
                rowToRetain = group[0];
            }
            else
            {
                // Apply retention strategy
                rowToRetain = options.RetentionStrategy switch
                {
                    RetentionStrategy.First => options.PreserveOriginalOrder
                        ? group.OrderBy(r => r.RowIndex).First()
                        : group.First(),
                    RetentionStrategy.Last => options.PreserveOriginalOrder
                        ? group.OrderByDescending(r => r.RowIndex).First()
                        : group.Last(),
                    _ => throw new NotSupportedException($"Retention strategy {options.RetentionStrategy} is not supported")
                };
            }

            retainedRows.Add(rowToRetain);

            processedGroups++;

            // Report progress
            if (processedGroups % 100 == 0 || processedGroups == totalGroups)
            {
                var percentComplete = 60 + (int)((double)processedGroups / totalGroups * 30); // 60-90%
                progressCallback?.Report(new DuplicateRemovalProgress(
                    percentComplete,
                    "Selecting rows to retain",
                    processedGroups,
                    totalGroups));
            }
        }

        // Sort by original row index if preserving order
        if (options.PreserveOriginalOrder)
        {
            retainedRows = retainedRows.OrderBy(r => r.RowIndex).ToList();
        }

        return retainedRows;
    }

    private static DataSetData CreateProcessedDataSet(DataSetData originalData, List<DataSetRowData> retainedRows, DuplicateRemovalOptions options)
    {
        // Create new rows with sequential indices
        var processedRows = retainedRows.Select((row, index) =>
            new DataSetRowData(index, row.Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)))
            .ToList();

        return new DataSetData(originalData.Columns, processedRows);
    }
}