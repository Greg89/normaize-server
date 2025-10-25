using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Application.Interfaces;

/// <summary>
/// Service for loading dataset data for processing operations
/// </summary>
public interface IDataSetDataLoader
{
    /// <summary>
    /// Loads the raw data for a dataset
    /// </summary>
    /// <param name="dataSetId">ID of the dataset to load</param>
    /// <returns>The dataset data as a collection of rows</returns>
    Task<DataSetData> LoadDataSetDataAsync(Guid dataSetId);

    /// <summary>
    /// Loads a subset of dataset data for preview or sampling
    /// </summary>
    /// <param name="dataSetId">ID of the dataset to load</param>
    /// <param name="maxRows">Maximum number of rows to load</param>
    /// <returns>Sample of the dataset data</returns>
    Task<DataSetData> LoadDataSetSampleAsync(Guid dataSetId, int maxRows = 1000);

    /// <summary>
    /// Gets the column metadata for a dataset
    /// </summary>
    /// <param name="dataSetId">ID of the dataset</param>
    /// <returns>Column information</returns>
    Task<IReadOnlyList<DataSetColumn>> GetDataSetColumnsAsync(Guid dataSetId);
}

/// <summary>
/// Service for persisting processed dataset results
/// </summary>
public interface IDataSetDataPersister
{
    /// <summary>
    /// Saves processed dataset data, replacing the original
    /// </summary>
    /// <param name="dataSetId">ID of the dataset</param>
    /// <param name="processedData">The processed data to save</param>
    /// <param name="operation">The operation that was performed</param>
    /// <returns>True if successful</returns>
    Task<bool> SaveProcessedDataAsync(Guid dataSetId, DataSetData processedData, string operation);

    /// <summary>
    /// Creates a backup of the original dataset before processing
    /// </summary>
    /// <param name="dataSetId">ID of the dataset to backup</param>
    /// <returns>Backup ID for potential rollback</returns>
    Task<string> CreateBackupAsync(Guid dataSetId);

    /// <summary>
    /// Restores a dataset from a backup
    /// </summary>
    /// <param name="dataSetId">ID of the dataset to restore</param>
    /// <param name="backupId">ID of the backup to restore from</param>
    /// <returns>True if successful</returns>
    Task<bool> RestoreFromBackupAsync(Guid dataSetId, string backupId);
}

/// <summary>
/// Represents the actual data content of a dataset for processing operations
/// </summary>
public class DataSetData
{
    public IReadOnlyList<DataSetColumn> Columns { get; init; } = Array.Empty<DataSetColumn>();
    public IReadOnlyList<DataSetRowData> Rows { get; init; } = Array.Empty<DataSetRowData>();
    public int TotalRows => Rows.Count;
    public int TotalColumns => Columns.Count;
    public long EstimatedSizeBytes { get; init; }
    
    public DataSetData(IEnumerable<DataSetColumn> columns, IEnumerable<DataSetRowData> rows)
    {
        Columns = columns.ToList().AsReadOnly();
        Rows = rows.ToList().AsReadOnly();
        EstimatedSizeBytes = CalculateEstimatedSize();
    }

    private long CalculateEstimatedSize()
    {
        // Simple estimation: assume average 50 bytes per cell
        return TotalRows * TotalColumns * 50L;
    }
}

/// <summary>
/// Represents a column in a dataset
/// </summary>
public class DataSetColumn
{
    public string Name { get; init; } = string.Empty;
    public string DataType { get; init; } = "string";
    public int Index { get; init; }
    public bool AllowNull { get; init; } = true;
    
    public DataSetColumn(string name, string dataType, int index, bool allowNull = true)
    {
        Name = name;
        DataType = dataType;
        Index = index;
        AllowNull = allowNull;
    }
}

/// <summary>
/// Represents a row of data in a dataset for processing operations
/// </summary>
public class DataSetRowData
{
    public int RowIndex { get; init; }
    public IReadOnlyDictionary<string, object?> Values { get; init; }
    
    public DataSetRowData(int rowIndex, Dictionary<string, object?> values)
    {
        RowIndex = rowIndex;
        Values = values.AsReadOnly();
    }

    public T? GetValue<T>(string columnName)
    {
        if (Values.TryGetValue(columnName, out var value))
        {
            if (value is T typedValue)
                return typedValue;
            
            // Try to convert
            try
            {
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
        return default(T);
    }
}