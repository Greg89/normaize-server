using System;
using System.Collections.Generic;
using System.Linq;
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

            return ExtractColumnsFromDataSet(dataSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dataset columns for {DataSetId}", dataSetId);
            throw;
        }
    }

    private static DataSetData ConvertToDataSetData(DataSet dataSet, IEnumerable<Domain.Entities.DataSetRow> rows)
    {
        var columns = ExtractColumnsFromDataSet(dataSet);
        var convertedRows = ConvertRowsToDataSetRows(rows, columns);

        return new DataSetData(columns, convertedRows);
    }

    private static IReadOnlyList<DataSetColumn> ExtractColumnsFromDataSet(DataSet dataSet)
    {
        var columns = new List<DataSetColumn>();

        // Extract column information from dataset statistics
        // For now, create generic columns based on the total column count
        for (int i = 0; i < dataSet.Statistics.ColumnCount; i++)
        {
            columns.Add(new DataSetColumn(
                name: $"Column{i + 1}",
                dataType: "string",
                index: i,
                allowNull: true));
        }

        return columns.AsReadOnly();
    }

    private static IReadOnlyList<Application.Interfaces.DataSetRowData> ConvertRowsToDataSetRows(
        IEnumerable<Domain.Entities.DataSetRow> entityRows,
        IReadOnlyList<DataSetColumn> columns)
    {
        var convertedRows = new List<Application.Interfaces.DataSetRowData>();

        foreach (var entityRow in entityRows)
        {
            var values = entityRow.GetAllValues();
            convertedRows.Add(new Application.Interfaces.DataSetRowData(entityRow.RowIndex, values));
        }

        return convertedRows.AsReadOnly();
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

    public async Task<string> CreateBackupAsync(Guid dataSetId)
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
            return backupId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for dataset {DataSetId}", dataSetId);
            throw;
        }
    }

    public async Task<bool> RestoreFromBackupAsync(Guid dataSetId, string backupId)
    {
        _logger.LogInformation("Restoring dataset {DataSetId} from backup {BackupId}", dataSetId, backupId);

        try
        {
            // TODO: Implement actual restore logic
            // This would involve restoring data from the backup location

            _logger.LogInformation("Successfully restored dataset {DataSetId} from backup {BackupId}", dataSetId, backupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore dataset {DataSetId} from backup {BackupId}", dataSetId, backupId);
            return false;
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