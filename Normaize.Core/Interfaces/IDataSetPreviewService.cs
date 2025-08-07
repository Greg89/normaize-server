using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for dataset preview and schema operations.
/// </summary>
public interface IDataSetPreviewService
{
    /// <summary>
    /// Get preview data for a dataset.
    /// </summary>
    Task<DataSetPreviewDto?> GetDataSetPreviewAsync(int id, int rows, string userId);
    
    /// <summary>
    /// Get schema information for a dataset.
    /// </summary>
    Task<object?> GetDataSetSchemaAsync(int id, string userId);
} 