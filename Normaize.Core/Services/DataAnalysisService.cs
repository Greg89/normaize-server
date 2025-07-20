using AutoMapper;
using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Normaize.Core.Services;

/// <summary>
/// Service for managing data analysis operations with chaos engineering resilience.
/// Implements industry-standard error handling and distributed tracing.
/// </summary>
public class DataAnalysisService : IDataAnalysisService
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<DataAnalysisService> _logger;
    private readonly IStructuredLoggingService _structuredLogging;
    private readonly IChaosEngineeringService _chaosEngineering;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);
    private readonly Random _random = new();

    public DataAnalysisService(
        IAnalysisRepository analysisRepository,
        IMapper mapper,
        ILogger<DataAnalysisService> logger,
        IStructuredLoggingService structuredLogging,
        IChaosEngineeringService chaosEngineering)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _structuredLogging = structuredLogging ?? throw new ArgumentNullException(nameof(structuredLogging));
        _chaosEngineering = chaosEngineering ?? throw new ArgumentNullException(nameof(chaosEngineering));
    }

    public async Task<AnalysisDto> CreateAnalysisAsync(CreateAnalysisDto createDto)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(CreateAnalysisAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["AnalysisName"] = createDto?.Name ?? AppConstants.Messages.UNKNOWN,
                ["AnalysisType"] = createDto?.Type.ToString() ?? AppConstants.Messages.UNKNOWN,
                ["DataSetId"] = createDto?.DataSetId ?? 0
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            ValidateCreateAnalysisDto(createDto!);
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate analysis creation failure
            await _chaosEngineering.ExecuteChaosAsync("AnalysisCreationFailure", correlationId, context.OperationName, () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating analysis creation failure", new Dictionary<string, object>
                {
                    ["ChaosType"] = "AnalysisCreationFailure"
                });
                throw new InvalidOperationException("Simulated analysis creation failure (chaos engineering)");
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
            var analysis = _mapper.Map<Analysis>(createDto);
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);

            _structuredLogging.LogStep(context, "Database save started");
            var savedAnalysis = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.AddAsync(analysis),
                _defaultTimeout,
                correlationId,
                $"{context.OperationName}_SaveToDatabase");
            _structuredLogging.LogStep(context, "Database save completed", new Dictionary<string, object>
            {
                ["AnalysisId"] = savedAnalysis.Id
            });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
            var result = _mapper.Map<AnalysisDto>(savedAnalysis);
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for analysis '{createDto?.Name}'", ex);
        }
    }

    public async Task<AnalysisDto?> GetAnalysisAsync(int id)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetAnalysisAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASET_ID] = id
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            if (id <= 0)
            {
                throw new ArgumentException(AppConstants.ValidationMessages.ANALYSIS_ID_MUST_BE_POSITIVE, nameof(id));
            }
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate database timeout
            await _chaosEngineering.ExecuteChaosAsync("DatabaseTimeout", correlationId, context.OperationName, async () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating database timeout", new Dictionary<string, object>
                {
                    ["ChaosType"] = "DatabaseTimeout"
                });
                await Task.Delay(15000); // 15 second delay
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
            var analysis = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.GetByIdAsync(id),
                TimeSpan.FromSeconds(30),
                correlationId,
                $"{context.OperationName}_GetFromDatabase");
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED);

            if (analysis == null)
            {
                _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND, new Dictionary<string, object>
                {
                    ["AnalysisId"] = id
                });
                _structuredLogging.LogSummary(context, false, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND);
                return null;
            }

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
            var result = _mapper.Map<AnalysisDto>(analysis);
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for analysis ID {id}", ex);
        }
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByDataSetAsync(int dataSetId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetAnalysesByDataSetAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["DataSetId"] = dataSetId
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            if (dataSetId <= 0)
            {
                throw new ArgumentException("Dataset ID must be positive", nameof(dataSetId));
            }
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate network latency
            await _chaosEngineering.ExecuteChaosAsync("NetworkLatency", correlationId, context.OperationName, async () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating network latency", new Dictionary<string, object>
                {
                    ["ChaosType"] = "NetworkLatency"
                });
                await Task.Delay(_random.Next(500, 2000));
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
            var analyses = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.GetByDataSetIdAsync(dataSetId),
                TimeSpan.FromSeconds(30),
                correlationId,
                $"{context.OperationName}_GetFromDatabase");
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
            {
                ["AnalysisCount"] = analyses.Count()
            });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
            var result = _mapper.Map<IEnumerable<AnalysisDto>>(analyses);
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for dataset ID {dataSetId}", ex);
        }
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByStatusAsync(AnalysisStatus status)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetAnalysesByStatusAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["Status"] = status.ToString()
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            if (!Enum.IsDefined(typeof(AnalysisStatus), status))
            {
                throw new ArgumentException(string.Format(AppConstants.ValidationMessages.INVALID_ANALYSIS_STATUS, status), nameof(status));
            }
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate cache failure
            await _chaosEngineering.ExecuteChaosAsync("CacheFailure", correlationId, context.OperationName, () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating cache failure", new Dictionary<string, object>
                {
                    ["ChaosType"] = "CacheFailure"
                });
                throw new InvalidOperationException("Simulated cache failure (chaos engineering)");
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
            var analyses = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.GetByStatusAsync(status),
                TimeSpan.FromSeconds(30),
                correlationId,
                $"{context.OperationName}_GetFromDatabase");
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
            {
                ["AnalysisCount"] = analyses.Count()
            });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
            var result = _mapper.Map<IEnumerable<AnalysisDto>>(analyses);
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for status {status}", ex);
        }
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByTypeAsync(AnalysisType type)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetAnalysesByTypeAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["Type"] = type.ToString()
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            if (!Enum.IsDefined(typeof(AnalysisType), type))
            {
                throw new ArgumentException(string.Format(AppConstants.ValidationMessages.INVALID_ANALYSIS_TYPE, type), nameof(type));
            }
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate storage failure
            await _chaosEngineering.ExecuteChaosAsync("StorageFailure", correlationId, context.OperationName, () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating storage failure", new Dictionary<string, object>
                {
                    ["ChaosType"] = "StorageFailure"
                });
                throw new InvalidOperationException("Simulated storage failure (chaos engineering)");
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
            var analyses = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.GetByTypeAsync(type),
                TimeSpan.FromSeconds(30),
                correlationId,
                $"{context.OperationName}_GetFromDatabase");
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
            {
                ["AnalysisCount"] = analyses.Count()
            });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
            var result = _mapper.Map<IEnumerable<AnalysisDto>>(analyses);
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for type {type}", ex);
        }
    }

    public async Task<AnalysisResultDto> GetAnalysisResultAsync(int analysisId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetAnalysisResultAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["AnalysisId"] = analysisId
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            if (analysisId <= 0)
            {
                throw new ArgumentException(AppConstants.ValidationMessages.ANALYSIS_ID_MUST_BE_POSITIVE, nameof(analysisId));
            }
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate memory pressure
            await _chaosEngineering.ExecuteChaosAsync("MemoryPressure", correlationId, context.OperationName, async () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating memory pressure", new Dictionary<string, object>
                {
                    ["ChaosType"] = "MemoryPressure"
                });
                // Simulate memory pressure by allocating temporary objects
                var tempObjects = new List<byte[]>();
                for (int i = 0; i < 50; i++)
                {
                    tempObjects.Add(new byte[1024 * 1024]); // 1MB each
                }
                await Task.Delay(100);
                tempObjects.Clear();
                GC.Collect();
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
            var analysis = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.GetByIdAsync(analysisId),
                TimeSpan.FromSeconds(30),
                correlationId,
                $"{context.OperationName}_GetFromDatabase");
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED);

            if (analysis == null)
            {
                _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND, new Dictionary<string, object>
                {
                    ["AnalysisId"] = analysisId
                });
                _structuredLogging.LogSummary(context, false, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND);
                throw new ArgumentException($"Analysis with ID {analysisId} not found", nameof(analysisId));
            }

            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.RESULTS_DESERIALIZATION_STARTED);
            var deserializedResults = await DeserializeResultsSafelyAsync(analysis.Results, analysisId, correlationId);
            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.RESULTS_DESERIALIZATION_COMPLETED);

            var result = new AnalysisResultDto
            {
                AnalysisId = analysisId,
                Status = analysis.Status,
                Results = deserializedResults,
                ErrorMessage = analysis.ErrorMessage
            };
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for analysis ID {analysisId}", ex);
        }
    }

    public async Task<bool> DeleteAnalysisAsync(int id)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(DeleteAnalysisAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["AnalysisId"] = id
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            if (id <= 0)
            {
                throw new ArgumentException(AppConstants.ValidationMessages.ANALYSIS_ID_MUST_BE_POSITIVE, nameof(id));
            }
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate processing delay
            await _chaosEngineering.ExecuteChaosAsync("ProcessingDelay", correlationId, context.OperationName, async () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating processing delay", new Dictionary<string, object>
                {
                    ["ChaosType"] = "ProcessingDelay"
                });
                await Task.Delay(_random.Next(1000, 3000));
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.DATABASE_DELETION_STARTED);
            var result = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.DeleteAsync(id),
                TimeSpan.FromSeconds(30),
                correlationId,
                $"{context.OperationName}_DeleteFromDatabase");
            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.DATABASE_DELETION_COMPLETED, new Dictionary<string, object>
            {
                ["DeletionResult"] = result
            });
            
            if (result)
            {
                _structuredLogging.LogSummary(context, true);
            }
            else
            {
                _structuredLogging.LogSummary(context, false, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND_OR_DELETED);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for analysis ID {id}", ex);
        }
    }

    public async Task<AnalysisDto> RunAnalysisAsync(int analysisId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(RunAnalysisAsync), 
            correlationId, 
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["AnalysisId"] = analysisId
            });

        try
        {
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            if (analysisId <= 0)
            {
                throw new ArgumentException(AppConstants.ValidationMessages.ANALYSIS_ID_MUST_BE_POSITIVE, nameof(analysisId));
            }
            _structuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            // Chaos engineering: Simulate processing delay
            await _chaosEngineering.ExecuteChaosAsync("ProcessingDelay", correlationId, context.OperationName, async () =>
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating processing delay", new Dictionary<string, object>
                {
                    ["ChaosType"] = "ProcessingDelay"
                });
                await Task.Delay(_random.Next(1000, 5000));
            }, new Dictionary<string, object> { ["UserId"] = AppConstants.Auth.AnonymousUser });

            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
            var analysis = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.GetByIdAsync(analysisId),
                TimeSpan.FromSeconds(30),
                correlationId,
                $"{context.OperationName}_GetAnalysis");
            _structuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED);

            if (analysis == null)
            {
                _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND, new Dictionary<string, object>
                {
                    ["AnalysisId"] = analysisId
                });
                _structuredLogging.LogSummary(context, false, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND);
                throw new ArgumentException($"Analysis with ID {analysisId} not found", nameof(analysisId));
            }

            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_STATE_VALIDATION_STARTED);
            ValidateAnalysisState(analysis, analysisId, correlationId);
            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_STATE_VALIDATION_COMPLETED);

            // If already completed, return existing result
            if (analysis.Status == AnalysisStatus.Completed)
            {
                _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_ALREADY_COMPLETED, new Dictionary<string, object>
                {
                    ["AnalysisId"] = analysisId,
                    ["Status"] = analysis.Status.ToString()
                });
                _structuredLogging.LogSummary(context, true, AppConstants.AnalysisMessages.ANALYSIS_ALREADY_COMPLETED);
                return _mapper.Map<AnalysisDto>(analysis);
            }

            // Execute analysis with state management
            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_EXECUTION_STARTED, new Dictionary<string, object>
            {
                ["AnalysisType"] = analysis.Type.ToString(),
                ["AnalysisId"] = analysis.Id
            });
            var result = await ExecuteAnalysisWithStateManagementAsync(analysis, context);
            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_EXECUTION_COMPLETED);
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for analysis ID {analysisId}", ex);
        }
    }

    #region Private Methods

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

    private static void ValidateAnalysisState(Analysis analysis, int analysisId, string correlationId)
    {
        if (analysis.Status == AnalysisStatus.Processing)
        {
            throw new InvalidOperationException($"Analysis with ID {analysisId} is already in progress. CorrelationId: {correlationId}");
        }
    }

    private async Task<AnalysisDto> ExecuteAnalysisWithStateManagementAsync(Analysis analysis, IOperationContext context)
    {
        // Set processing state
        analysis.Status = AnalysisStatus.Processing;
        _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.SETTING_ANALYSIS_STATUS_TO_PROCESSING);
        await ExecuteWithTimeoutAsync(
            () => _analysisRepository.UpdateAsync(analysis),
            TimeSpan.FromSeconds(30),
            context.CorrelationId,
            $"{context.OperationName}_UpdateStatus");

        try
        {
            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_EXECUTION_STARTED, new Dictionary<string, object>
            {
                ["AnalysisType"] = analysis.Type.ToString(),
                ["AnalysisId"] = analysis.Id
            });

            // Execute analysis with timeout
            var results = await ExecuteWithTimeoutAsync(
                () => ExecuteAnalysisByTypeAsync(analysis),
                _defaultTimeout,
                context.CorrelationId,
                $"{context.OperationName}_ExecuteAnalysis");

            // Update with success state
            analysis.Status = AnalysisStatus.Completed;
            analysis.CompletedAt = DateTime.UtcNow;
            analysis.Results = JsonSerializer.Serialize(results);
            analysis.ErrorMessage = null;

            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_COMPLETED_SUCCESSFULLY, new Dictionary<string, object>
            {
                ["AnalysisId"] = analysis.Id,
                ["Status"] = analysis.Status.ToString()
            });

            var updatedAnalysis = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.UpdateAsync(analysis),
                TimeSpan.FromSeconds(30),
                context.CorrelationId,
                $"{context.OperationName}_UpdateSuccess");

            return _mapper.Map<AnalysisDto>(updatedAnalysis);
        }
        catch (Exception ex)
        {
            // Update with failure state
            analysis.Status = AnalysisStatus.Failed;
            analysis.ErrorMessage = ex.Message;
            
            _structuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_FAILED, new Dictionary<string, object>
            {
                ["AnalysisId"] = analysis.Id,
                ["ErrorMessage"] = ex.Message
            });
            
            await ExecuteWithTimeoutAsync(
                () => _analysisRepository.UpdateAsync(analysis),
                TimeSpan.FromSeconds(30),
                context.CorrelationId,
                $"{context.OperationName}_UpdateFailure");
            
            throw new InvalidOperationException($"Failed to execute analysis of type {analysis.Type} for ID {analysis.Id}", ex);
        }
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private async Task<object?> DeserializeResultsSafelyAsync(string? results, int analysisId, string correlationId)
    {
        if (string.IsNullOrEmpty(results))
            return null;

        try
        {
            return await Task.Run(() => JsonSerializer.Deserialize<object>(results));
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Failed to deserialize results for analysis ID: {AnalysisId}. CorrelationId: {CorrelationId}", 
                analysisId, correlationId);
            return null;
        }
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

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Analysis Execution Methods

    private async Task<object> ExecuteNormalizationAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing normalization analysis for ID: {AnalysisId}", analysis.Id);
        
        // FUTURE: Implement normalization logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "Normalization",
            Message = "Data normalization completed",
            NormalizedColumns = new[] { AppConstants.DataStructures.CUSTOMER_ID, AppConstants.DataStructures.ORDER_AMOUNT },
            MinValues = new { customer_id = 0.0, order_amount = 0.0 },
            MaxValues = new { customer_id = 1.0, order_amount = 1.0 }
        };
    }

    private async Task<object> ExecuteComparisonAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing comparison analysis for ID: {AnalysisId}", analysis.Id);
        
        // FUTURE: Implement comparison logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "Comparison",
            Message = "Dataset comparison completed",
            SimilarityScore = 0.85,
            Differences = new[] { AppConstants.DataStructures.CUSTOMER_ID, "product_code" },
            CommonColumns = new[] { AppConstants.DataStructures.ORDER_AMOUNT, "sales_region" }
        };
    }

    private async Task<object> ExecuteStatisticalAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing statistical analysis for ID: {AnalysisId}", analysis.Id);
        
        // FUTURE: Implement statistical analysis logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "Statistical",
            Message = "Statistical analysis completed",
            Mean = new { customer_id = 45.2, order_amount = 78.9 },
            Median = new { customer_id = 42.0, order_amount = 75.0 },
            StandardDeviation = new { customer_id = 12.5, order_amount = 15.3 }
        };
    }

    private async Task<object> ExecuteDataCleaningAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing data cleaning analysis for ID: {AnalysisId}", analysis.Id);
        
        // FUTURE: Implement data cleaning logic
        await Task.Delay(1000); // Simulate processing time
        
        return new
        {
            Type = "DataCleaning",
            Message = "Data cleaning completed",
            RemovedRows = 15,
            FixedNullValues = 8,
            RemovedDuplicates = 3,
            CleanedColumns = new[] { AppConstants.DataStructures.CUSTOMER_ID, AppConstants.DataStructures.ORDER_AMOUNT, "product_code" }
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
            OutlierColumns = new[] { AppConstants.DataStructures.CUSTOMER_ID, AppConstants.DataStructures.ORDER_AMOUNT },
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
                customer_id_order_amount = 0.75,
                customer_id_product_code = -0.32,
                order_amount_product_code = 0.18
            },
            StrongCorrelations = new[] { "customer_id-order_amount" },
            WeakCorrelations = new[] { "order_amount-product_code" }
        };
    }

    private async Task<object> ExecuteTrendAnalysisAsync(Analysis analysis)
    {
        _logger.LogDebug("Executing trend analysis for ID: {AnalysisId}", analysis.Id);
        
        // FUTURE: Implement trend analysis logic
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

    #endregion
} 


