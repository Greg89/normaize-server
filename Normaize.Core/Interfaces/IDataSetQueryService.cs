using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for dataset querying, searching, and filtering operations.
/// </summary>
public interface IDataSetQueryService
{
    /// <summary>
    /// Get datasets by user with pagination.
    /// </summary>
    Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get deleted datasets by user with pagination.
    /// </summary>
    Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Search datasets by term with pagination.
    /// </summary>
    Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get datasets by file type with pagination.
    /// </summary>
    Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(FileType fileType, string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get datasets by date range with pagination.
    /// </summary>
    Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get dataset statistics for a user.
    /// </summary>
    Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId);
}