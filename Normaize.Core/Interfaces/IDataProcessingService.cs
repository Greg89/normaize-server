using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

public interface IDataProcessingService
{
    Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto);
    Task<DataSetDto?> GetDataSetAsync(int id, string userId);
    Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId);
    Task<bool> DeleteDataSetAsync(int id, string userId);
    Task<bool> RestoreDataSetAsync(int id, string userId);
    Task<bool> HardDeleteDataSetAsync(int id, string userId);
    Task<string?> GetDataSetPreviewAsync(int id, int rows, string userId);
    Task<object?> GetDataSetSchemaAsync(int id, string userId);
    Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId);
    Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId);
    Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(FileType fileType, string userId);
    Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId);
    Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId);
} 