using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

public interface IDataProcessingService
{
    Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto);
    Task<DataSetDto?> GetDataSetAsync(int id);
    Task<IEnumerable<DataSetDto>> GetAllDataSetsAsync();
    Task<bool> DeleteDataSetAsync(int id);
    Task<string?> GetDataSetPreviewAsync(int id, int rows = 10);
    Task<object?> GetDataSetSchemaAsync(int id);
} 