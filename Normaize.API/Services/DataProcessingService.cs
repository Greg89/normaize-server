using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data.Repositories;
using System.Globalization;
using System.Text.Json;

namespace Normaize.API.Services;

public class DataProcessingService : IDataProcessingService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IMapper _mapper;
    private readonly ILogger<DataProcessingService> _logger;

    public DataProcessingService(
        IDataSetRepository dataSetRepository,
        IFileUploadService fileUploadService,
        IMapper mapper,
        ILogger<DataProcessingService> logger)
    {
        _dataSetRepository = dataSetRepository;
        _fileUploadService = fileUploadService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        try
        {
            // Validate file
            if (!await _fileUploadService.ValidateFileAsync(fileRequest))
            {
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = "Invalid file format or size"
                };
            }

            // Save file
            var filePath = await _fileUploadService.SaveFileAsync(fileRequest);

            // Process file and create dataset
            var dataSet = await _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest.FileName));
            
            // Update with user-provided information
            dataSet.Name = createDto.Name;
            dataSet.Description = createDto.Description;

            // Save to database
            var savedDataSet = await _dataSetRepository.AddAsync(dataSet);

            return new DataSetUploadResponse
            {
                DataSetId = savedDataSet.Id,
                Success = true,
                Message = "Dataset uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset");
            return new DataSetUploadResponse
            {
                Success = false,
                Message = "Error uploading dataset: " + ex.Message
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        return _mapper.Map<DataSetDto>(dataSet);
    }

    public async Task<IEnumerable<DataSetDto>> GetAllDataSetsAsync()
    {
        var dataSets = await _dataSetRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<DataSetDto>>(dataSets);
    }

    public async Task<bool> DeleteDataSetAsync(int id)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null)
            return false;

        // Delete the file
        if (!string.IsNullOrEmpty(dataSet.FileName))
        {
            await _fileUploadService.DeleteFileAsync(dataSet.FileName);
        }

        return await _dataSetRepository.DeleteAsync(id);
    }

    public async Task<string?> GetDataSetPreviewAsync(int id, int rows = 10)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null || string.IsNullOrEmpty(dataSet.PreviewData))
            return null;

        return dataSet.PreviewData;
    }

    public async Task<object?> GetDataSetSchemaAsync(int id)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);
        if (dataSet == null || string.IsNullOrEmpty(dataSet.Schema))
            return null;

        return JsonSerializer.Deserialize<object>(dataSet.Schema);
    }
} 