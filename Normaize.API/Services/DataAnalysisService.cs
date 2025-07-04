using AutoMapper;
using Microsoft.Extensions.Logging;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data.Repositories;

namespace Normaize.API.Services;

public class DataAnalysisService : IDataAnalysisService
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<DataAnalysisService> _logger;

    public DataAnalysisService(
        IAnalysisRepository analysisRepository,
        IMapper mapper,
        ILogger<DataAnalysisService> logger)
    {
        _analysisRepository = analysisRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AnalysisDto> CreateAnalysisAsync(CreateAnalysisDto createDto)
    {
        var analysis = _mapper.Map<Analysis>(createDto);
        var savedAnalysis = await _analysisRepository.AddAsync(analysis);
        return _mapper.Map<AnalysisDto>(savedAnalysis);
    }

    public async Task<AnalysisDto?> GetAnalysisAsync(int id)
    {
        var analysis = await _analysisRepository.GetByIdAsync(id);
        return _mapper.Map<AnalysisDto>(analysis);
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByDataSetAsync(int dataSetId)
    {
        var analyses = await _analysisRepository.GetByDataSetIdAsync(dataSetId);
        return _mapper.Map<IEnumerable<AnalysisDto>>(analyses);
    }

    public async Task<AnalysisResultDto> GetAnalysisResultAsync(int analysisId)
    {
        var analysis = await _analysisRepository.GetByIdAsync(analysisId);
        if (analysis == null)
            throw new ArgumentException($"Analysis with ID {analysisId} not found");

        return new AnalysisResultDto
        {
            AnalysisId = analysisId,
            Status = analysis.Status,
            Results = analysis.Results != null ? System.Text.Json.JsonSerializer.Deserialize<object>(analysis.Results) : null,
            ErrorMessage = analysis.ErrorMessage
        };
    }

    public async Task<bool> DeleteAnalysisAsync(int id)
    {
        return await _analysisRepository.DeleteAsync(id);
    }

    public async Task<AnalysisDto> RunAnalysisAsync(int analysisId)
    {
        var analysis = await _analysisRepository.GetByIdAsync(analysisId);
        if (analysis == null)
            throw new ArgumentException($"Analysis with ID {analysisId} not found");

        try
        {
            analysis.Status = "Processing";
            await _analysisRepository.UpdateAsync(analysis);

            // TODO: Implement actual analysis logic based on type
            await Task.Delay(1000); // Simulate processing time

            analysis.Status = "Completed";
            analysis.CompletedAt = DateTime.UtcNow;
            analysis.Results = "{\"message\": \"Analysis completed successfully\"}";
            
            var updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);
            return _mapper.Map<AnalysisDto>(updatedAnalysis);
        }
        catch (Exception ex)
        {
            analysis.Status = "Failed";
            analysis.ErrorMessage = ex.Message;
            await _analysisRepository.UpdateAsync(analysis);
            throw;
        }
    }
} 