using AutoMapper;
using Microsoft.Extensions.Logging;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Text.Json;

namespace Normaize.Core.Services;

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
        try
        {
            _logger.LogInformation("Creating new analysis: {AnalysisName} of type {AnalysisType}", 
                createDto.Name, createDto.Type);

            // Validate input
            ValidateCreateAnalysisDto(createDto);

            var analysis = _mapper.Map<Analysis>(createDto);
            var savedAnalysis = await _analysisRepository.AddAsync(analysis);
            
            _logger.LogInformation("Successfully created analysis with ID: {AnalysisId}", savedAnalysis.Id);
            return _mapper.Map<AnalysisDto>(savedAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating analysis: {AnalysisName}", createDto.Name);
            throw;
        }
    }

    public async Task<AnalysisDto?> GetAnalysisAsync(int id)
    {
        try
        {
            _logger.LogDebug("Retrieving analysis with ID: {AnalysisId}", id);
            
            var analysis = await _analysisRepository.GetByIdAsync(id);
            if (analysis == null)
            {
                _logger.LogWarning("Analysis with ID {AnalysisId} not found", id);
                return null;
            }

            return _mapper.Map<AnalysisDto>(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis with ID: {AnalysisId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByDataSetAsync(int dataSetId)
    {
        try
        {
            _logger.LogDebug("Retrieving analyses for dataset ID: {DataSetId}", dataSetId);
            
            var analyses = await _analysisRepository.GetByDataSetIdAsync(dataSetId);
            var analysisDtos = _mapper.Map<IEnumerable<AnalysisDto>>(analyses);
            
            _logger.LogDebug("Retrieved {Count} analyses for dataset ID: {DataSetId}", 
                analysisDtos.Count(), dataSetId);
            
            return analysisDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses for dataset ID: {DataSetId}", dataSetId);
            throw;
        }
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByStatusAsync(AnalysisStatus status)
    {
        try
        {
            _logger.LogDebug("Retrieving analyses with status: {Status}", status);
            
            var analyses = await _analysisRepository.GetByStatusAsync(status);
            var analysisDtos = _mapper.Map<IEnumerable<AnalysisDto>>(analyses);
            
            _logger.LogDebug("Retrieved {Count} analyses with status: {Status}", 
                analysisDtos.Count(), status);
            
            return analysisDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses with status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByTypeAsync(AnalysisType type)
    {
        try
        {
            _logger.LogDebug("Retrieving analyses with type: {Type}", type);
            
            var analyses = await _analysisRepository.GetByTypeAsync(type);
            var analysisDtos = _mapper.Map<IEnumerable<AnalysisDto>>(analyses);
            
            _logger.LogDebug("Retrieved {Count} analyses with type: {Type}", 
                analysisDtos.Count(), type);
            
            return analysisDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses with type: {Type}", type);
            throw;
        }
    }

    public async Task<AnalysisResultDto> GetAnalysisResultAsync(int analysisId)
    {
        try
        {
            _logger.LogDebug("Retrieving analysis result for ID: {AnalysisId}", analysisId);
            
            var analysis = await _analysisRepository.GetByIdAsync(analysisId);
            if (analysis == null)
            {
                _logger.LogWarning("Analysis with ID {AnalysisId} not found", analysisId);
                throw new ArgumentException($"Analysis with ID {analysisId} not found");
            }

            object? deserializedResults = null;
            if (!string.IsNullOrEmpty(analysis.Results))
            {
                try
                {
                    deserializedResults = JsonSerializer.Deserialize<object>(analysis.Results);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx, "Failed to deserialize results for analysis ID: {AnalysisId}", analysisId);
                    // Continue with null results rather than failing the entire request
                }
            }

            return new AnalysisResultDto
            {
                AnalysisId = analysisId,
                Status = analysis.Status,
                Results = deserializedResults,
                ErrorMessage = analysis.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis result for ID: {AnalysisId}", analysisId);
            throw;
        }
    }

    public async Task<bool> DeleteAnalysisAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting analysis with ID: {AnalysisId}", id);
            
            var result = await _analysisRepository.DeleteAsync(id);
            
            if (result)
            {
                _logger.LogInformation("Successfully deleted analysis with ID: {AnalysisId}", id);
            }
            else
            {
                _logger.LogWarning("Failed to delete analysis with ID: {AnalysisId} - not found or already deleted", id);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting analysis with ID: {AnalysisId}", id);
            throw;
        }
    }

    public async Task<AnalysisDto> RunAnalysisAsync(int analysisId)
    {
        try
        {
            _logger.LogInformation("Starting analysis execution for ID: {AnalysisId}", analysisId);
            
            var analysis = await _analysisRepository.GetByIdAsync(analysisId);
            if (analysis == null)
            {
                _logger.LogWarning("Analysis with ID {AnalysisId} not found", analysisId);
                throw new ArgumentException($"Analysis with ID {analysisId} not found");
            }

            // Check if analysis is already in progress or completed
            if (analysis.Status == AnalysisStatus.Processing)
            {
                _logger.LogWarning("Analysis with ID {AnalysisId} is already in progress", analysisId);
                throw new InvalidOperationException($"Analysis with ID {analysisId} is already in progress");
            }

            if (analysis.Status == AnalysisStatus.Completed)
            {
                _logger.LogInformation("Analysis with ID {AnalysisId} is already completed", analysisId);
                return _mapper.Map<AnalysisDto>(analysis);
            }

            try
            {
                analysis.Status = AnalysisStatus.Processing;
                await _analysisRepository.UpdateAsync(analysis);

                _logger.LogInformation("Executing analysis of type {AnalysisType} for ID: {AnalysisId}", 
                    analysis.Type, analysisId);

                // Execute analysis based on type
                var results = await ExecuteAnalysisByTypeAsync(analysis);

                analysis.Status = AnalysisStatus.Completed;
                analysis.CompletedAt = DateTime.UtcNow;
                analysis.Results = JsonSerializer.Serialize(results);
                analysis.ErrorMessage = null; // Clear any previous errors
                
                var updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);
                
                _logger.LogInformation("Successfully completed analysis with ID: {AnalysisId}", analysisId);
                return _mapper.Map<AnalysisDto>(updatedAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing analysis with ID: {AnalysisId}", analysisId);
                
                analysis.Status = AnalysisStatus.Failed;
                analysis.ErrorMessage = ex.Message;
                await _analysisRepository.UpdateAsync(analysis);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RunAnalysisAsync for ID: {AnalysisId}", analysisId);
            throw;
        }
    }

    private static void ValidateCreateAnalysisDto(CreateAnalysisDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        if (string.IsNullOrWhiteSpace(createDto.Name))
            throw new ArgumentException("Analysis name is required", nameof(createDto));

        if (createDto.Name.Length > 255)
            throw new ArgumentException("Analysis name cannot exceed 255 characters", nameof(createDto));

        if (createDto.Description?.Length > 1000)
            throw new ArgumentException("Analysis description cannot exceed 1000 characters", nameof(createDto));

        if (createDto.DataSetId <= 0)
            throw new ArgumentException("Valid dataset ID is required", nameof(createDto));
    }

    private async Task<object> ExecuteAnalysisByTypeAsync(Analysis analysis)
    {
        return analysis.Type switch
        {
            AnalysisType.Normalization => await ExecuteNormalizationAnalysisAsync(analysis),
            AnalysisType.Comparison => await ExecuteComparisonAnalysisAsync(analysis),
            AnalysisType.Statistical => await ExecuteStatisticalAnalysisAsync(analysis),
            AnalysisType.DataCleaning => await ExecuteDataCleaningAnalysisAsync(analysis),
            AnalysisType.OutlierDetection => await ExecuteOutlierDetectionAnalysisAsync(analysis),
            AnalysisType.CorrelationAnalysis => await ExecuteCorrelationAnalysisAsync(analysis),
            AnalysisType.TrendAnalysis => await ExecuteTrendAnalysisAsync(analysis),
            AnalysisType.Custom => await ExecuteCustomAnalysisAsync(analysis),
            _ => throw new NotSupportedException($"Analysis type {analysis.Type} is not supported")
        };
    }

    private async Task<object> ExecuteNormalizationAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing normalization analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement normalization logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "Normalization",
            Message = "Data normalization completed",
            NormalizedColumns = new[] { "column1", "column2" },
            MinValues = new { column1 = 0.0, column2 = 0.0 },
            MaxValues = new { column1 = 1.0, column2 = 1.0 }
        };
    }

    private async Task<object> ExecuteComparisonAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing comparison analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement comparison logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "Comparison",
            Message = "Dataset comparison completed",
            SimilarityScore = 0.85,
            Differences = new[] { "column1", "column3" },
            CommonColumns = new[] { "column2", "column4" }
        };
    }

    private async Task<object> ExecuteStatisticalAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing statistical analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement statistical analysis logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "Statistical",
            Message = "Statistical analysis completed",
            Mean = new { column1 = 45.2, column2 = 78.9 },
            Median = new { column1 = 42.0, column2 = 75.0 },
            StandardDeviation = new { column1 = 12.5, column2 = 15.3 }
        };
    }

    private async Task<object> ExecuteDataCleaningAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing data cleaning analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement data cleaning logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "DataCleaning",
            Message = "Data cleaning completed",
            RemovedRows = 15,
            FixedNullValues = 8,
            RemovedDuplicates = 3,
            CleanedColumns = new[] { "column1", "column2", "column3" }
        };
    }

    private async Task<object> ExecuteOutlierDetectionAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing outlier detection analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement outlier detection logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "OutlierDetection",
            Message = "Outlier detection completed",
            DetectedOutliers = 7,
            OutlierColumns = new[] { "column1", "column2" },
            OutlierIndices = new[] { 15, 23, 45, 67, 89, 123, 156 }
        };
    }

    private async Task<object> ExecuteCorrelationAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing correlation analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement correlation analysis logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "CorrelationAnalysis",
            Message = "Correlation analysis completed",
            CorrelationMatrix = new
            {
                column1_column2 = 0.75,
                column1_column3 = -0.32,
                column2_column3 = 0.18
            },
            StrongCorrelations = new[] { "column1-column2" },
            WeakCorrelations = new[] { "column2-column3" }
        };
    }

    private async Task<object> ExecuteTrendAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing trend analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement trend analysis logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "TrendAnalysis",
            Message = "Trend analysis completed",
            TrendDirection = "Increasing",
            TrendStrength = 0.82,
            SeasonalPatterns = true,
            Forecast = new[] { 45.2, 46.1, 47.3, 48.5 }
        };
    }

    private async Task<object> ExecuteCustomAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing custom analysis for ID: {AnalysisId}", analysis.Id);
        
        // TODO: Implement custom analysis logic based on configuration
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "Custom",
            Message = "Custom analysis completed",
            CustomMetrics = new { metric1 = 123.45, metric2 = "custom_value" },
            ProcessingTime = "1.2s",
            CustomConfiguration = analysis.Configuration
        };
    }
} 