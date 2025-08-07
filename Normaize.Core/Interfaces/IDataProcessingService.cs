using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Core service for dataset CRUD operations and file processing.
/// </summary>
public interface IDataProcessingService
{
    /// <summary>
    /// Upload and process a new dataset.
    /// </summary>
    Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto);
    
    /// <summary>
    /// Get a dataset by ID.
    /// </summary>
    Task<DataSetDto?> GetDataSetAsync(int id, string userId);
    
    /// <summary>
    /// Update a dataset.
    /// </summary>
    Task<DataSetDto?> UpdateDataSetAsync(int id, UpdateDataSetDto updateDto, string userId);
    
    /// <summary>
    /// Soft delete a dataset.
    /// </summary>
    Task<bool> DeleteDataSetAsync(int id, string userId);
}