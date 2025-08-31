using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;

namespace Normaize.Core.Services.DataNormalization;

/// <summary>
/// Processor for removing duplicate rows from datasets
/// </summary>
public class DuplicateRowRemovalProcessor : IDuplicateRowRemovalProcessor
{
    private readonly ILogger<DuplicateRowRemovalProcessor> _logger;
    private readonly IDataSetRowRepository _dataSetRowRepository;

    public DuplicateRowRemovalProcessor(
        ILogger<DuplicateRowRemovalProcessor> logger,
        IDataSetRowRepository dataSetRowRepository)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dataSetRowRepository);

        _logger = logger;
        _dataSetRowRepository = dataSetRowRepository;
    }

    public async Task<NormalizationResults> ProcessAsync(
        DataSet dataSet,
        RemoveDuplicateRowsRequest request,
        IProgress<int> progressCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(progressCallback);

        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);

        try
        {
            _logger.LogInformation("Starting duplicate row removal for dataset {DataSetId}. Columns: {Columns}, KeepFirst: {KeepFirst}, CaseSensitive: {CaseSensitive}",
                dataSet.Id, string.Join(", ", request.ColumnNames), request.KeepFirstOccurrence, request.CaseSensitive);

            // Validate request
            var validationResult = await ValidateRequestAsync(dataSet, request);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            // Report initial progress
            progressCallback.Report(5);
            cancellationToken.ThrowIfCancellationRequested();

            // Load dataset rows
            var rows = await LoadDatasetRowsAsync(dataSet, cancellationToken);
            progressCallback.Report(15);
            cancellationToken.ThrowIfCancellationRequested();

            // Analyze dataset structure
            var columnIndices = AnalyzeDatasetStructure(dataSet, request.ColumnNames);
            progressCallback.Report(25);
            cancellationToken.ThrowIfCancellationRequested();

            // Remove duplicates
            var (uniqueRows, duplicateCount) = await RemoveDuplicateRowsAsync(
                rows,
                columnIndices,
                request,
                progressCallback,
                cancellationToken);

            // Update dataset
            await UpdateDatasetAsync(dataSet, uniqueRows, cancellationToken);
            progressCallback.Report(95);
            cancellationToken.ThrowIfCancellationRequested();

            // Finalize
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsageMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

            var results = new NormalizationResults
            {
                RowsProcessed = rows.Count,
                DuplicateRowsRemoved = duplicateCount,
                RowsRemaining = uniqueRows.Count,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                MemoryUsageMB = memoryUsageMB
            };

            progressCallback.Report(100);
            stopwatch.Stop();

            _logger.LogInformation("Duplicate row removal completed for dataset {DataSetId}. Removed {DuplicateCount} duplicates, {RemainingCount} rows remaining. Time: {TimeMs}ms, Memory: {MemoryMB:F2}MB",
                dataSet.Id, duplicateCount, uniqueRows.Count, results.ProcessingTimeMs, results.MemoryUsageMB);

            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Duplicate row removal cancelled for dataset {DataSetId}", dataSet.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during duplicate row removal for dataset {DataSetId}", dataSet.Id);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public Task<long> EstimateProcessingTimeAsync(DataSet dataSet, RemoveDuplicateRowsRequest request)
    {
        // Simple estimation based on row count and complexity
        var baseTimePerRow = 0.1; // milliseconds per row
        var complexityMultiplier = request.ColumnNames.Length * 0.5 + 1.0;
        var estimatedTime = (long)(dataSet.RowCount * baseTimePerRow * complexityMultiplier);

        // Add overhead for large datasets
        if (dataSet.RowCount > DataNormalizationConstants.DataNormalization.MAX_ROWS_FOR_SYNC_PROCESSING)
        {
            estimatedTime += 5000; // 5 seconds overhead for large datasets
        }

        return Task.FromResult(estimatedTime);
    }

    public Task<double> EstimateMemoryUsageAsync(DataSet dataSet, RemoveDuplicateRowsRequest request)
    {
        // Estimate memory usage based on row count and column count
        var bytesPerRow = dataSet.ColumnCount * 100; // Assume average 100 bytes per column value
        var estimatedBytes = dataSet.RowCount * bytesPerRow;
        var estimatedMB = estimatedBytes / (1024.0 * 1024.0);

        // Add buffer for processing overhead
        estimatedMB *= 1.5;

        return Task.FromResult(Math.Min(estimatedMB, DataNormalizationConstants.DataNormalization.MAX_MEMORY_USAGE_MB));
    }

    public async Task<NormalizationValidationResult> ValidateRequestAsync(DataSet dataSet, RemoveDuplicateRowsRequest request)
    {
        var warnings = new List<string>();

        // Validate column names
        var columnValidationResult = ValidateColumnNames(request);
        if (!columnValidationResult.IsValid)
        {
            return columnValidationResult;
        }

        // Validate dataset state
        var datasetValidationResult = ValidateDatasetState(dataSet);
        if (!datasetValidationResult.IsValid)
        {
            return datasetValidationResult;
        }

        // Validate column existence
        var columnExistenceResult = ValidateColumnExistence(dataSet, request);
        if (!columnExistenceResult.IsValid)
        {
            return columnExistenceResult;
        }

        // Add warnings from column existence validation
        warnings.AddRange(columnExistenceResult.Warnings);

        // Check memory requirements
        var estimatedMemory = await EstimateMemoryUsageAsync(dataSet, request);
        if (estimatedMemory > DataNormalizationConstants.DataNormalization.WARNING_MEMORY_USAGE_MB)
        {
            warnings.Add($"Estimated memory usage ({estimatedMemory:F2}MB) exceeds warning threshold");
        }

        // Check processing time
        var estimatedTime = await EstimateProcessingTimeAsync(dataSet, request);
        if (estimatedTime > DataNormalizationConstants.DataNormalization.DEFAULT_PROCESSING_TIMEOUT_MS)
        {
            warnings.Add($"Estimated processing time ({estimatedTime}ms) exceeds default timeout");
        }

        return warnings.Any()
            ? NormalizationValidationResult.SuccessWithWarnings(warnings)
            : NormalizationValidationResult.Success();
    }

    private NormalizationValidationResult ValidateColumnNames(RemoveDuplicateRowsRequest request)
    {
        if (request.ColumnNames == null || request.ColumnNames.Length == 0)
        {
            return NormalizationValidationResult.Failure(DataNormalizationConstants.DataNormalization.AT_LEAST_ONE_COLUMN_REQUIRED);
        }

        if (request.ColumnNames.Length > DataNormalizationConstants.DataNormalization.MAX_COLUMNS_FOR_DUPLICATE_DETECTION)
        {
            return NormalizationValidationResult.Failure($"Maximum {DataNormalizationConstants.DataNormalization.MAX_COLUMNS_FOR_DUPLICATE_DETECTION} columns allowed for duplicate detection");
        }

        return NormalizationValidationResult.Success();
    }

    private NormalizationValidationResult ValidateDatasetState(DataSet dataSet)
    {
        if (!dataSet.IsProcessed)
        {
            return NormalizationValidationResult.Failure(DataNormalizationConstants.DataNormalization.DATASET_MUST_BE_PROCESSED);
        }

        if (dataSet.RowCount == 0)
        {
            return NormalizationValidationResult.Failure("Dataset has no rows to process");
        }

        return NormalizationValidationResult.Success();
    }

    private NormalizationValidationResult ValidateColumnExistence(DataSet dataSet, RemoveDuplicateRowsRequest request)
    {
        var warnings = new List<string>();

        if (string.IsNullOrEmpty(dataSet.Schema))
        {
            return NormalizationValidationResult.Success();
        }

        try
        {
            var schema = JsonSerializer.Deserialize<Dictionary<string, object>>(dataSet.Schema);
            if (schema != null)
            {
                var missingColumns = request.ColumnNames.Where(col => !schema.ContainsKey(col)).ToList();
                if (missingColumns.Any())
                {
                    return NormalizationValidationResult.Failure($"Columns not found in dataset: {string.Join(", ", missingColumns)}");
                }
            }
        }
        catch (JsonException)
        {
            warnings.Add("Unable to validate column existence - proceeding with caution");
        }

        return warnings.Any()
            ? NormalizationValidationResult.SuccessWithWarnings(warnings)
            : NormalizationValidationResult.Success();
    }

    private async Task<List<DataSetRow>> LoadDatasetRowsAsync(DataSet dataSet, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading rows for dataset {DataSetId}", dataSet.Id);

        if (dataSet.UseSeparateTable)
        {
            // Load from separate table for large datasets
            var rows = await _dataSetRowRepository.GetByDataSetIdAsync(dataSet.Id);
            return rows.ToList();
        }
        else
        {
            // Load from embedded data for small datasets
            if (string.IsNullOrEmpty(dataSet.ProcessedData))
            {
                throw new InvalidOperationException("Dataset has no processed data available");
            }

            try
            {
                var rows = JsonSerializer.Deserialize<List<DataSetRow>>(dataSet.ProcessedData);
                return rows ?? new List<DataSetRow>();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to deserialize dataset processed data", ex);
            }
        }
    }

    private Dictionary<string, int> AnalyzeDatasetStructure(DataSet dataSet, string[] columnNames)
    {
        if (string.IsNullOrEmpty(dataSet.Schema))
        {
            throw new InvalidOperationException("Dataset schema not available");
        }

        try
        {
            var schema = JsonSerializer.Deserialize<Dictionary<string, object>>(dataSet.Schema);
            if (schema == null)
            {
                throw new InvalidOperationException("Failed to deserialize dataset schema");
            }

            var columnIndices = new Dictionary<string, int>();
            var schemaKeys = schema.Keys.ToList();

            foreach (var columnName in columnNames)
            {
                var index = schemaKeys.IndexOf(columnName);
                if (index == -1)
                {
                    throw new InvalidOperationException($"Column '{columnName}' not found in dataset");
                }
                columnIndices[columnName] = index;
            }

            return columnIndices;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse dataset schema", ex);
        }
    }

    private Task<(List<DataSetRow> uniqueRows, int duplicateCount)> RemoveDuplicateRowsAsync(
        List<DataSetRow> rows,
        Dictionary<string, int> columnIndices,
        RemoveDuplicateRowsRequest request,
        IProgress<int> progressCallback,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Removing duplicate rows. Total rows: {TotalRows}, Columns: {Columns}",
            rows.Count, string.Join(", ", columnIndices.Keys));

        var uniqueRows = new List<DataSetRow>();
        var seenKeys = new HashSet<string>();
        var duplicateCount = 0;
        var processedRows = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var rowData = JsonSerializer.Deserialize<List<object>>(row.Data);
                if (rowData == null) continue;

                var key = CreateDuplicateKey(rowData, columnIndices, request.CaseSensitive);

                if (seenKeys.Add(key))
                {
                    uniqueRows.Add(row);
                }
                else
                {
                    duplicateCount++;

                    // If keeping last occurrence, remove the previous one and add this one
                    if (!request.KeepFirstOccurrence)
                    {
                        uniqueRows.RemoveAt(uniqueRows.Count - 1);
                        uniqueRows.Add(row);
                    }
                }

                processedRows++;
                if (processedRows % 100 == 0)
                {
                    var progress = 25 + (int)((processedRows / (double)rows.Count) * 60);
                    progressCallback.Report(progress);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse row data for row {RowId}", row.Id);
                // Skip malformed rows
                continue;
            }
        }

        return Task.FromResult((uniqueRows, duplicateCount));
    }

    private string CreateDuplicateKey(List<object> rowData, Dictionary<string, int> columnIndices, bool caseSensitive)
    {
        var keyParts = new List<string>();

        foreach (var columnName in columnIndices.Keys)
        {
            var columnIndex = columnIndices[columnName];
            if (columnIndex < rowData.Count)
            {
                var value = rowData[columnIndex]?.ToString() ?? "";
                keyParts.Add(caseSensitive ? value : value.ToLowerInvariant());
            }
            else
            {
                keyParts.Add(""); // Missing column value
            }
        }

        return string.Join("|", keyParts);
    }

    private async Task UpdateDatasetAsync(DataSet dataSet, List<DataSetRow> uniqueRows, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating dataset {DataSetId} with {UniqueRowCount} unique rows", dataSet.Id, uniqueRows.Count);

        // Update row count
        dataSet.RowCount = uniqueRows.Count;
        dataSet.LastModifiedAt = DateTime.UtcNow;

        if (dataSet.UseSeparateTable)
        {
            // For separate table, we need to delete old rows and add new ones
            // This is a simplified approach - in production, you might want to use transactions
            await _dataSetRowRepository.DeleteByDataSetIdAsync(dataSet.Id);
            await _dataSetRowRepository.AddRangeAsync(uniqueRows);
        }
        else
        {
            // Update embedded data
            dataSet.ProcessedData = JsonSerializer.Serialize(uniqueRows);
        }

        // Update preview data if it exists
        if (!string.IsNullOrEmpty(dataSet.PreviewData))
        {
            try
            {
                var previewRows = JsonSerializer.Deserialize<List<object>>(dataSet.PreviewData);
                if (previewRows != null)
                {
                    var updatedPreview = uniqueRows.Take(100).Select(r => r.Data).ToList();
                    dataSet.PreviewData = JsonSerializer.Serialize(updatedPreview);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to update preview data for dataset {DataSetId}", dataSet.Id);
            }
        }
    }
}
