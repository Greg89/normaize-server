using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Implementation of data loading using our DDD repositories
/// </summary>
public class DataSetDataLoader : IDataSetDataLoader
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IDataSetRowRepository _rowRepository;
    private readonly ILogger<DataSetDataLoader> _logger;

    public DataSetDataLoader(
        IDataSetRepository dataSetRepository,
        IDataSetRowRepository rowRepository,
        ILogger<DataSetDataLoader> logger)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _rowRepository = rowRepository ?? throw new ArgumentNullException(nameof(rowRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DataSetData> LoadDataSetDataAsync(Guid dataSetId)
    {
        _logger.LogInformation("Loading complete dataset data for {DataSetId}", dataSetId);

        try
        {
            var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
            if (dataSet == null)
            {
                throw new InvalidOperationException($"Dataset {dataSetId} not found");
            }

            // Prefer processed JSON payloads if present (small datasets / legacy path)
            var processedRows = TryExtractRowsFromJsonPayload(dataSet.ProcessedData, maxRows: int.MaxValue);
            if (processedRows.Count > 0)
            {
                var columns = ExtractColumnsFromDataSet(dataSet, processedRows);
                return new DataSetData(columns, processedRows);
            }

            // Fall back to separate rows table
            var rows = await _rowRepository.GetByDataSetIdAsync(dataSetId);
            return ConvertToDataSetData(dataSet, rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dataset data for {DataSetId}", dataSetId);
            throw;
        }
    }

    public async Task<DataSetData> LoadDataSetSampleAsync(Guid dataSetId, int maxRows = 1000)
    {
        _logger.LogInformation("Loading sample dataset data for {DataSetId}, max rows: {MaxRows}", dataSetId, maxRows);

        try
        {
            var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
            if (dataSet == null)
            {
                throw new InvalidOperationException($"Dataset {dataSetId} not found");
            }

            // Primary source for preview: DataSet.PreviewData generated at upload/processing time.
            // This fixes the common case where DataSetRows were never persisted.
            var previewRows = TryExtractRowsFromPreviewPayload(dataSet.PreviewData, maxRows);
            if (previewRows.Count > 0)
            {
                var columns = ExtractColumnsFromDataSet(dataSet, previewRows);
                return new DataSetData(columns, previewRows);
            }

            // Fall back to rows table
            var rows = await _rowRepository.GetByDataSetIdAsync(dataSetId, 0, maxRows);
            return ConvertToDataSetData(dataSet, rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dataset sample for {DataSetId}", dataSetId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DataSetColumn>> GetDataSetColumnsAsync(Guid dataSetId)
    {
        _logger.LogInformation("Loading column metadata for {DataSetId}", dataSetId);

        try
        {
            var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
            if (dataSet == null)
            {
                throw new InvalidOperationException($"Dataset {dataSetId} not found");
            }

            return ExtractColumnsFromDataSet(dataSet, parsedRows: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dataset columns for {DataSetId}", dataSetId);
            throw;
        }
    }

    private static DataSetData ConvertToDataSetData(DataSet dataSet, IEnumerable<Domain.Entities.DataSetRow> rows)
    {
        var columns = ExtractColumnsFromDataSet(dataSet, parsedRows: null);
        var convertedRows = ConvertRowsToDataSetRows(rows);

        return new DataSetData(columns, convertedRows);
    }

    private static IReadOnlyList<DataSetColumn> ExtractColumnsFromDataSet(
        DataSet dataSet,
        IReadOnlyList<Application.Interfaces.DataSetRowData>? parsedRows)
    {
        // 1) Best: parse schema JSON produced by FileProcessingService
        var schemaColumns = TryExtractColumnsFromSchema(dataSet.Schema);
        if (schemaColumns.Count > 0)
        {
            return schemaColumns;
        }

        // 2) Next: columns can be embedded in preview payload
        var previewColumns = TryExtractColumnsFromPreviewPayload(dataSet.PreviewData);
        if (previewColumns.Count > 0)
        {
            return previewColumns;
        }

        // 3) Next: infer from parsed preview/processed rows
        if (parsedRows is { Count: > 0 })
        {
            var firstRow = parsedRows[0].Values;
            var inferred = firstRow.Keys
                .Select((name, index) => new DataSetColumn(name, "string", index, allowNull: true))
                .ToList();
            if (inferred.Count > 0)
            {
                return inferred.AsReadOnly();
            }
        }

        // 4) Fallback: generic columns based on statistics
        var fallbackCount = Math.Max(dataSet.Statistics.ColumnCount, 0);
        var columns = new List<DataSetColumn>(capacity: fallbackCount);
        for (int i = 0; i < fallbackCount; i++)
        {
            columns.Add(new DataSetColumn($"Column{i + 1}", "string", i, allowNull: true));
        }

        return columns.AsReadOnly();
    }

    private static IReadOnlyList<Application.Interfaces.DataSetRowData> ConvertRowsToDataSetRows(
        IEnumerable<Domain.Entities.DataSetRow> entityRows)
    {
        var convertedRows = new List<Application.Interfaces.DataSetRowData>();

        foreach (var entityRow in entityRows)
        {
            var values = entityRow.GetAllValues();
            convertedRows.Add(new Application.Interfaces.DataSetRowData(entityRow.RowIndex, values));
        }

        return convertedRows.AsReadOnly();
    }

    private static IReadOnlyList<DataSetColumn> TryExtractColumnsFromSchema(string? schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return Array.Empty<DataSetColumn>();
        }

        try
        {
            using var doc = JsonDocument.Parse(schema);
            var root = doc.RootElement;

            if (!TryGetPropertyIgnoreCase(root, "Columns", out var columnsElement) ||
                columnsElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<DataSetColumn>();
            }

            var columns = new List<DataSetColumn>();
            var index = 0;
            foreach (var col in columnsElement.EnumerateArray())
            {
                // schema shape: { Columns: [ { Name, Type }, ... ] }
                var name = TryGetPropertyIgnoreCase(col, "Name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                    ? nameEl.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var dataType = TryGetPropertyIgnoreCase(col, "Type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
                    ? typeEl.GetString()
                    : "string";

                columns.Add(new DataSetColumn(name!, dataType ?? "string", index, allowNull: true));
                index++;
            }

            return columns.Count > 0 ? columns.AsReadOnly() : Array.Empty<DataSetColumn>();
        }
        catch
        {
            return Array.Empty<DataSetColumn>();
        }
    }

    private static IReadOnlyList<DataSetColumn> TryExtractColumnsFromPreviewPayload(string? previewData)
    {
        if (string.IsNullOrWhiteSpace(previewData))
        {
            return Array.Empty<DataSetColumn>();
        }

        try
        {
            using var doc = JsonDocument.Parse(previewData);
            var root = doc.RootElement;

            if (!TryGetPropertyIgnoreCase(root, "Columns", out var columnsEl) ||
                columnsEl.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<DataSetColumn>();
            }

            // preview shape: { Columns: [ "A", "B" ] } (or sometimes array of objects)
            var columns = new List<DataSetColumn>();
            var index = 0;
            foreach (var item in columnsEl.EnumerateArray())
            {
                string? name = item.ValueKind switch
                {
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Object when TryGetPropertyIgnoreCase(item, "Name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String => nameEl.GetString(),
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                columns.Add(new DataSetColumn(name!, "string", index, allowNull: true));
                index++;
            }

            return columns.Count > 0 ? columns.AsReadOnly() : Array.Empty<DataSetColumn>();
        }
        catch
        {
            return Array.Empty<DataSetColumn>();
        }
    }

    private static IReadOnlyList<Application.Interfaces.DataSetRowData> TryExtractRowsFromPreviewPayload(string? previewData, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(previewData) || maxRows <= 0)
        {
            return Array.Empty<Application.Interfaces.DataSetRowData>();
        }

        try
        {
            using var doc = JsonDocument.Parse(previewData);
            var root = doc.RootElement;

            if (!TryGetPropertyIgnoreCase(root, "Rows", out var rowsEl) || rowsEl.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<Application.Interfaces.DataSetRowData>();
            }

            var take = Math.Min(maxRows, rowsEl.GetArrayLength());
            var rowsJson = rowsEl.GetRawText();
            var dictionaries = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(rowsJson) ?? new();

            var results = new List<Application.Interfaces.DataSetRowData>(capacity: take);
            for (var i = 0; i < Math.Min(take, dictionaries.Count); i++)
            {
                results.Add(new Application.Interfaces.DataSetRowData(i, dictionaries[i]));
            }

            return results.AsReadOnly();
        }
        catch
        {
            return Array.Empty<Application.Interfaces.DataSetRowData>();
        }
    }

    private static IReadOnlyList<Application.Interfaces.DataSetRowData> TryExtractRowsFromJsonPayload(string? jsonPayload, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload) || maxRows <= 0)
        {
            return Array.Empty<Application.Interfaces.DataSetRowData>();
        }

        // Accept either an array of row objects OR the same wrapped preview shape.
        try
        {
            using var doc = JsonDocument.Parse(jsonPayload);
            var root = doc.RootElement;

            JsonElement rowsEl;
            if (root.ValueKind == JsonValueKind.Array)
            {
                rowsEl = root;
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     TryGetPropertyIgnoreCase(root, "Rows", out var wrappedRows) &&
                     wrappedRows.ValueKind == JsonValueKind.Array)
            {
                rowsEl = wrappedRows;
            }
            else
            {
                return Array.Empty<Application.Interfaces.DataSetRowData>();
            }

            var take = Math.Min(maxRows, rowsEl.GetArrayLength());
            var dictionaries = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(rowsEl.GetRawText()) ?? new();

            var results = new List<Application.Interfaces.DataSetRowData>(capacity: take);
            for (var i = 0; i < Math.Min(take, dictionaries.Count); i++)
            {
                results.Add(new Application.Interfaces.DataSetRowData(i, dictionaries[i]));
            }

            return results.AsReadOnly();
        }
        catch
        {
            return Array.Empty<Application.Interfaces.DataSetRowData>();
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

/// <summary>
/// Implementation of data persistence using our DDD repositories
/// </summary>
public class DataSetDataPersister : IDataSetDataPersister
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IDataSetRowRepository _rowRepository;
    private readonly ILogger<DataSetDataPersister> _logger;

    public DataSetDataPersister(
        IDataSetRepository dataSetRepository,
        IDataSetRowRepository rowRepository,
        ILogger<DataSetDataPersister> logger)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _rowRepository = rowRepository ?? throw new ArgumentNullException(nameof(rowRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SaveProcessedDataAsync(Guid dataSetId, DataSetData processedData, string operation)
    {
        _logger.LogInformation("Saving processed data for dataset {DataSetId}, operation: {Operation}", dataSetId, operation);

        try
        {
            // Get the existing dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
            if (dataSet == null)
            {
                throw new InvalidOperationException($"Dataset {dataSetId} not found");
            }

            // Delete existing rows
            await _rowRepository.DeleteByDataSetIdAsync(dataSetId);

            // Convert and save new rows
            var entityRows = ConvertToEntityRows(processedData, dataSetId);
            await _rowRepository.SaveRangeAsync(entityRows);

            // Update dataset metadata
            dataSet.MarkAsProcessed();
            await _dataSetRepository.UpdateAsync(dataSet);

            _logger.LogInformation("Successfully saved {RowCount} processed rows for dataset {DataSetId}",
                processedData.TotalRows, dataSetId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save processed data for dataset {DataSetId}", dataSetId);
            return false;
        }
    }

    public Task<string> CreateBackupAsync(Guid dataSetId)
    {
        _logger.LogInformation("Creating backup for dataset {DataSetId}", dataSetId);

        try
        {
            // For now, we'll use timestamp-based backup IDs
            // In a real implementation, you'd store backups in a separate table or storage
            var backupId = $"backup_{dataSetId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            // TODO: Implement actual backup logic
            // This could involve copying data to a backup table or storage location

            _logger.LogInformation("Created backup {BackupId} for dataset {DataSetId}", backupId, dataSetId);
            return Task.FromResult(backupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for dataset {DataSetId}", dataSetId);
            throw;
        }
    }

    public Task<bool> RestoreFromBackupAsync(Guid dataSetId, string backupId)
    {
        _logger.LogInformation("Restoring dataset {DataSetId} from backup {BackupId}", dataSetId, backupId);

        try
        {
            // TODO: Implement actual restore logic
            // This would involve restoring data from the backup location

            _logger.LogInformation("Successfully restored dataset {DataSetId} from backup {BackupId}", dataSetId, backupId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore dataset {DataSetId} from backup {BackupId}", dataSetId, backupId);
            return Task.FromResult(false);
        }
    }

    private static IEnumerable<Domain.Entities.DataSetRow> ConvertToEntityRows(DataSetData processedData, Guid dataSetId)
    {
        var entityRows = new List<Domain.Entities.DataSetRow>();

        foreach (var row in processedData.Rows)
        {
            // Convert row values to JSON for storage
            var jsonData = System.Text.Json.JsonSerializer.Serialize(row.Values);

            entityRows.Add(Domain.Entities.DataSetRow.Create(dataSetId, row.RowIndex, jsonData));
        }

        return entityRows;
    }
}