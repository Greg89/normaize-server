using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Mapping;
using Normaize.Core.Configuration;
using System.Text.Json;
using System.Diagnostics;

namespace Normaize.Core.Services;

/// <summary>
/// Service for dataset preview and schema operations.
/// </summary>
public class DataSetPreviewService : IDataSetPreviewService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public DataSetPreviewService(
        IDataSetRepository dataSetRepository,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(infrastructure);
        _dataSetRepository = dataSetRepository;
        _infrastructure = infrastructure;
    }

    public async Task<DataSetPreviewDto?> GetDataSetPreviewAsync(int id, int rows, string userId)
    {
        return await ExecutePreviewOperationAsync(
            AppConstants.DataSetPreview.GET_DATA_SET_PREVIEW,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id, ["Rows"] = rows },
            () => ValidatePreviewInputs(id, rows, userId),
            async (context) =>
            {
                // Chaos engineering: Simulate memory pressure during preview data processing
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("MemoryPressure", context.CorrelationId, context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating memory pressure during preview data processing", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "MemoryPressure",
                        ["DataSetId"] = id,
                        ["RequestedRows"] = rows
                    });
                    
                    // Simulate memory pressure by allocating temporary objects
                    var tempObjects = new List<byte[]>();
                    for (int i = 0; i < AppConstants.ChaosEngineering.MEMORY_PRESSURE_OBJECT_COUNT; i++)
                    {
                        tempObjects.Add(new byte[AppConstants.ChaosEngineering.MEMORY_PRESSURE_OBJECT_SIZE_BYTES]); // 1MB each
                    }
                    await Task.Delay(AppConstants.ChaosEngineering.MEMORY_PRESSURE_DELAY_MS);
                    tempObjects.Clear();
                }, new Dictionary<string, object> { ["UserId"] = userId, ["DataSetId"] = id, ["Rows"] = rows });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                
                if (string.IsNullOrEmpty(dataSet.PreviewData))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.NO_PREVIEW_DATA_AVAILABLE);
                    return null;
                }
                
                try
                {
                    // Deserialize as DataSetPreviewDto format (standardized format)
                    var previewData = JsonSerializer.Deserialize<DataSetPreviewDto>(dataSet.PreviewData, JsonConfiguration.DefaultOptions);
                    
                    if (previewData == null || previewData.Rows == null)
                    {
                        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.NO_PREVIEW_DATA_AVAILABLE);
                        return null;
                    }
                    
                    // Limit the number of rows returned
                    var limitedRows = previewData.Rows.Take(rows).ToList();
                    previewData.Rows = limitedRows;
                    previewData.PreviewRowCount = rows;
                    
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.PREVIEW_DATA_RETRIEVED_SUCCESSFULLY, new Dictionary<string, object>
                    {
                        ["RequestedRows"] = rows,
                        ["ActualRows"] = limitedRows.Count,
                        ["TotalAvailableRows"] = previewData.TotalRows
                    });
                    
                    return previewData;
                }
                catch (JsonException ex)
                {
                    _infrastructure.StructuredLogging.LogException(ex, AppConstants.DataSetPreview.FAILED_TO_DESERIALIZE_PREVIEW_DATA);
                    return null;
                }
            });
    }

    public async Task<object?> GetDataSetSchemaAsync(int id, string userId)
    {
        return await ExecutePreviewOperationAsync(
            AppConstants.DataSetPreview.GET_DATA_SET_SCHEMA,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id },
            () => ValidateSchemaInputs(id, userId),
            async (context) =>
            {
                // Chaos engineering: Simulate processing delay during schema retrieval
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("ProcessingDelay", context.CorrelationId, context.OperationName, async () =>
                {
                    var delayMs = new Random().Next(AppConstants.ChaosEngineering.MIN_PROCESSING_DELAY_MS, AppConstants.ChaosEngineering.MAX_PROCESSING_DELAY_MS);
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating processing delay during schema retrieval", new Dictionary<string, object>
                    {
                        ["DelayMs"] = delayMs,
                        ["ChaosType"] = "ProcessingDelay"
                    });
                    await Task.Delay(delayMs);
                }, new Dictionary<string, object> { ["UserId"] = userId, ["DataSetId"] = id });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                
                if (string.IsNullOrEmpty(dataSet.Schema))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.NO_SCHEMA_DATA_AVAILABLE);
                    return null;
                }
                
                return await DeserializeSchemaSafelyAsync(dataSet.Schema, context);
            });
    }

    #region Private Helper Methods

    private async Task<T> ExecutePreviewOperationAsync<T>(
        string operationName,
        string userId,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(
            operationName,
            correlationId,
            userId,
            additionalMetadata);

        try
        {
            validation();
            
            _infrastructure.StructuredLogging.LogStep(context, $"{operationName} started");
            
            var result = await operation(context);
            
            _infrastructure.StructuredLogging.LogSummary(context, true, $"{operationName} completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogException(ex, $"{operationName} failed");
            throw;
        }
    }

    private async Task<DataSet?> RetrieveDataSetWithAccessControlAsync(int id, string userId, IOperationContext context)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        
        if (dataSet == null)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.DATASET_NOT_FOUND);
            throw new InvalidOperationException($"Dataset with ID {id} not found");
        }
        
        if (dataSet.UserId != userId)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER);
            throw new UnauthorizedAccessException($"{AppConstants.DataSetPreview.ACCESS_DENIED_TO_DATASET} {id}");
        }
        
        return dataSet;
    }

    private async Task<object?> DeserializeSchemaSafelyAsync(string schema, IOperationContext context)
    {
        try
        {
            // Try to deserialize as List<string> first (most common case)
            var schemaList = JsonSerializer.Deserialize<List<string>>(schema, JsonConfiguration.DefaultOptions);
            if (schemaList != null)
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.SCHEMA_DESERIALIZED_SUCCESSFULLY);
                return schemaList;
            }
            
            // Fallback to generic object deserialization
            var schemaObject = JsonSerializer.Deserialize<object>(schema, JsonConfiguration.DefaultOptions);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetPreview.SCHEMA_DESERIALIZED_SUCCESSFULLY);
            return schemaObject;
        }
        catch (JsonException ex)
        {
            _infrastructure.StructuredLogging.LogException(ex, AppConstants.DataSetPreview.FAILED_TO_DESERIALIZE_SCHEMA);
            return null;
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidatePreviewInputs(int id, int rows, string userId)
    {
        ValidateDataSetIdAndUserId(id, userId);
        if (rows <= 0) throw new ArgumentException(AppConstants.DataSetPreview.ROWS_MUST_BE_POSITIVE, nameof(rows));
        if (rows > AppConstants.DataSetPreview.MAX_PREVIEW_ROWS) throw new ArgumentException(AppConstants.DataSetPreview.ROWS_CANNOT_EXCEED_1000, nameof(rows));
    }

    private static void ValidateSchemaInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateDataSetIdAndUserId(int id, string userId)
    {
        if (id <= 0) throw new ArgumentException(AppConstants.DataSetPreview.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(AppConstants.DataSetPreview.USER_ID_CANNOT_BE_NULL_OR_EMPTY, nameof(userId));
    }

    #endregion
} 