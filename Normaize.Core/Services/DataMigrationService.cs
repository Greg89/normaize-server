using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using Normaize.Core.Configuration;
using System.Text.Json;

namespace Normaize.Core.Services;

/// <summary>
/// Service for handling data migrations and format standardization
/// </summary>
public class DataMigrationService : IDataMigrationService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public DataMigrationService(
        IDataSetRepository dataSetRepository,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(infrastructure);
        _dataSetRepository = dataSetRepository;
        _infrastructure = infrastructure;
    }

    /// <summary>
    /// Standardizes the PreviewData format for all datasets
    /// </summary>
    public async Task<int> StandardizePreviewDataFormatAsync()
    {
        var correlationId = Guid.NewGuid().ToString();
        var context = _infrastructure.StructuredLogging.CreateContext(
            "StandardizePreviewDataFormat",
            correlationId,
            "system",
            new Dictionary<string, object> { ["MigrationType"] = "PreviewDataStandardization" });

        try
        {
            _infrastructure.StructuredLogging.LogStep(context, "Starting PreviewData format standardization");

            // Get all datasets with PreviewData
            var dataSets = await _dataSetRepository.GetAllAsync();
            var dataSetsWithPreviewData = dataSets.Where(ds => !string.IsNullOrEmpty(ds.PreviewData)).ToList();

            _infrastructure.StructuredLogging.LogStep(context, $"Found {dataSetsWithPreviewData.Count} datasets with PreviewData to standardize");

            int standardizedCount = 0;

            foreach (var dataSet in dataSetsWithPreviewData)
            {
                try
                {
                    if (await StandardizeSingleDataSetPreviewDataAsync(dataSet))
                    {
                        standardizedCount++;
                        _infrastructure.StructuredLogging.LogStep(context, $"Standardized PreviewData for dataset {dataSet.Id}", 
                            new Dictionary<string, object> { ["DataSetId"] = dataSet.Id });
                    }
                }
                catch (Exception ex)
                {
                    _infrastructure.StructuredLogging.LogException(ex, $"Failed to standardize PreviewData for dataset {dataSet.Id}");
                }
            }

            _infrastructure.StructuredLogging.LogSummary(context, true, $"PreviewData standardization completed. {standardizedCount} datasets processed.");
            return standardizedCount;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogException(ex, "PreviewData standardization failed");
            throw;
        }
    }

    /// <summary>
    /// Standardizes the PreviewData format for a single dataset
    /// </summary>
    private async Task<bool> StandardizeSingleDataSetPreviewDataAsync(DataSet dataSet)
    {
        if (string.IsNullOrEmpty(dataSet.PreviewData))
            return false;

        try
        {
            // Check if already in correct format
            if (IsPreviewDataInCorrectFormat(dataSet.PreviewData))
                return false;

            // Convert old format to new format
            var standardizedPreviewData = ConvertPreviewDataToStandardFormat(dataSet.PreviewData, dataSet.RowCount);
            
            // Update the dataset
            dataSet.PreviewData = standardizedPreviewData;
            dataSet.LastModifiedAt = DateTime.UtcNow;
            dataSet.LastModifiedBy = "system";

            await _dataSetRepository.UpdateAsync(dataSet);
            return true;
        }
        catch (Exception)
        {
            // If conversion fails, we'll leave the data as-is and log the error
            return false;
        }
    }

    /// <summary>
    /// Checks if PreviewData is already in the correct format
    /// </summary>
    private static bool IsPreviewDataInCorrectFormat(string previewData)
    {
        try
        {
            var deserialized = JsonSerializer.Deserialize<DataSetPreviewDto>(previewData, JsonConfiguration.DefaultOptions);
            return deserialized != null && 
                   deserialized.Columns != null && 
                   deserialized.Rows != null &&
                   deserialized.TotalRows > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts old PreviewData format to new standardized format
    /// </summary>
    private static string ConvertPreviewDataToStandardFormat(string oldPreviewData, int totalRows)
    {
        try
        {
            // Try to deserialize as old format (array of records)
            var oldRecords = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(oldPreviewData, JsonConfiguration.DefaultOptions);
            
            if (oldRecords == null || oldRecords.Count == 0)
                throw new InvalidOperationException("No valid records found in old PreviewData format");

            // Convert to new format
            var standardizedPreviewData = new DataSetPreviewDto
            {
                Columns = oldRecords.FirstOrDefault()?.Keys.ToList() ?? new List<string>(),
                Rows = oldRecords,
                TotalRows = totalRows,
                MaxPreviewRows = 1000, // Default max preview rows
                PreviewRowCount = oldRecords.Count
            };

            return JsonSerializer.Serialize(standardizedPreviewData, JsonConfiguration.DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert PreviewData format: {ex.Message}", ex);
        }
    }
} 