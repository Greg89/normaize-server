using AutoMapper;
using Microsoft.Extensions.Logging;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Text.Json;

namespace Normaize.Core.Services;

public class DataProcessingService : IDataProcessingService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<DataProcessingService> _logger;

    public DataProcessingService(
        IDataSetRepository dataSetRepository,
        IFileUploadService fileUploadService,
        IAuditService auditService,
        IMapper mapper,
        ILogger<DataProcessingService> logger)
    {
        _dataSetRepository = dataSetRepository;
        _fileUploadService = fileUploadService;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        try
        {
            _logger.LogInformation("Starting file upload process for {FileName} by user {UserId}", 
                fileRequest.FileName, createDto.UserId);

            // Validate file
            _logger.LogInformation("Validating file {FileName}", fileRequest.FileName);
            if (!await _fileUploadService.ValidateFileAsync(fileRequest))
            {
                _logger.LogWarning("File validation failed for {FileName}", fileRequest.FileName);
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = "Invalid file format or size"
                };
            }
            _logger.LogInformation("File validation passed for {FileName}", fileRequest.FileName);

            // Save file
            _logger.LogInformation("Saving file {FileName} to storage", fileRequest.FileName);
            var filePath = await _fileUploadService.SaveFileAsync(fileRequest);
            _logger.LogInformation("File saved successfully to {FilePath}", filePath);

            // Process file and create dataset
            _logger.LogInformation("Processing file {FilePath}", filePath);
            var dataSet = await _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest.FileName));
            _logger.LogInformation("File processing completed. Rows: {RowCount}, Columns: {ColumnCount}, FileSize: {FileSize}", 
                dataSet.RowCount, dataSet.ColumnCount, dataSet.FileSize);
            
            // Update with user-provided information
            dataSet.Name = createDto.Name;
            dataSet.Description = createDto.Description;
            dataSet.UserId = createDto.UserId;

            // Save to database
            _logger.LogInformation("Saving dataset to database");
            var savedDataSet = await _dataSetRepository.AddAsync(dataSet);
            _logger.LogInformation("Dataset saved to database with ID {DataSetId}", savedDataSet.Id);

            // Log audit trail
            await _auditService.LogDataSetActionAsync(
                savedDataSet.Id,
                createDto.UserId,
                "Created",
                new { 
                    fileName = fileRequest.FileName,
                    fileSize = dataSet.FileSize,
                    rowCount = dataSet.RowCount,
                    columnCount = dataSet.ColumnCount,
                    filePath = filePath
                }
            );

            _logger.LogInformation("File upload completed successfully. Dataset ID: {DataSetId}, File: {FilePath}", 
                savedDataSet.Id, filePath);

            return new DataSetUploadResponse
            {
                DataSetId = savedDataSet.Id,
                Success = true,
                Message = "Dataset uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset {FileName} for user {UserId}", 
                fileRequest.FileName, createDto.UserId);
            return new DataSetUploadResponse
            {
                Success = false,
                Message = "Error uploading dataset: " + ex.Message
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id, string userId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet?.UserId != userId)
            return null;
        
        // Log audit trail for data access
        await _auditService.LogDataSetActionAsync(id, userId, "Viewed");
        
        return _mapper.Map<DataSetDto>(dataSet);
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId)
    {
        var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<DataSetDto>>(dataSets);
    }

    public async Task<bool> DeleteDataSetAsync(int id, string userId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null || dataSet.UserId != userId)
            return false;

        // Delete the file
        if (!string.IsNullOrEmpty(dataSet.FilePath))
        {
            await _fileUploadService.DeleteFileAsync(dataSet.FilePath);
        }

        var result = await _dataSetRepository.DeleteAsync(id);
        
        if (result)
        {
            // Log audit trail for soft delete
            await _auditService.LogDataSetActionAsync(
                id, 
                userId, 
                "Deleted",
                new { 
                    fileName = dataSet.FileName,
                    fileSize = dataSet.FileSize,
                    rowCount = dataSet.RowCount
                }
            );
        }

        return result;
    }

    public async Task<bool> RestoreDataSetAsync(int id, string userId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null || dataSet.UserId != userId)
            return false;

        var result = await _dataSetRepository.RestoreAsync(id);
        
        if (result)
        {
            // Log audit trail for restore
            await _auditService.LogDataSetActionAsync(id, userId, "Restored");
        }

        return result;
    }

    public async Task<bool> HardDeleteDataSetAsync(int id, string userId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null || dataSet.UserId != userId)
            return false;

        // Delete the file
        if (!string.IsNullOrEmpty(dataSet.FilePath))
        {
            await _fileUploadService.DeleteFileAsync(dataSet.FilePath);
        }

        var result = await _dataSetRepository.HardDeleteAsync(id);
        
        if (result)
        {
            // Log audit trail for hard delete
            await _auditService.LogDataSetActionAsync(
                id, 
                userId, 
                "HardDeleted",
                new { 
                    fileName = dataSet.FileName,
                    fileSize = dataSet.FileSize,
                    rowCount = dataSet.RowCount
                }
            );
        }

        return result;
    }

    public async Task<string?> GetDataSetPreviewAsync(int id, int rows, string userId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.PreviewData))
            return null;

        // Log audit trail for preview access
        await _auditService.LogDataSetActionAsync(id, userId, "Previewed", new { rows });

        return dataSet.PreviewData;
    }

    public async Task<object?> GetDataSetSchemaAsync(int id, string userId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.Schema))
            return null;

        return JsonSerializer.Deserialize<object>(dataSet.Schema);
    }

    public async Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId)
    {
        var dataSets = await _dataSetRepository.GetByUserIdAsync(userId, includeDeleted: true);
        return _mapper.Map<IEnumerable<DataSetDto>>(dataSets.Where(d => d.IsDeleted));
    }

    public async Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId)
    {
        var dataSets = await _dataSetRepository.SearchAsync(searchTerm, userId);
        return _mapper.Map<IEnumerable<DataSetDto>>(dataSets);
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(string fileType, string userId)
    {
        var dataSets = await _dataSetRepository.GetByFileTypeAsync(fileType, userId);
        return _mapper.Map<IEnumerable<DataSetDto>>(dataSets);
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId)
    {
        var dataSets = await _dataSetRepository.GetByDateRangeAsync(startDate, endDate, userId);
        return _mapper.Map<IEnumerable<DataSetDto>>(dataSets);
    }

    public async Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId)
    {
        var totalCount = await _dataSetRepository.GetTotalCountAsync(userId);
        var totalSize = await _dataSetRepository.GetTotalSizeAsync(userId);
        var recentlyModified = await _dataSetRepository.GetRecentlyModifiedAsync(userId, 5);

        return new DataSetStatisticsDto
        {
            TotalCount = totalCount,
            TotalSize = totalSize,
            RecentlyModified = _mapper.Map<IEnumerable<DataSetDto>>(recentlyModified)
        };
    }
} 