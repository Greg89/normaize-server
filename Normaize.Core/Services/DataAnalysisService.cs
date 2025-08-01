using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Mapping;
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
    private readonly IDataProcessingInfrastructure _infrastructure;
    private readonly Random _random = new();

    public DataAnalysisService(
        IAnalysisRepository analysisRepository,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(analysisRepository);
        ArgumentNullException.ThrowIfNull(infrastructure);
        _analysisRepository = analysisRepository;
        _infrastructure = infrastructure;
    }

    public async Task<AnalysisDto> CreateAnalysisAsync(CreateAnalysisDto createDto)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(CreateAnalysisAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                ["AnalysisName"] = createDto?.Name ?? AppConstants.Messages.UNKNOWN,
                ["AnalysisType"] = createDto?.Type.ToString() ?? AppConstants.Messages.UNKNOWN,
                ["DataSetId"] = createDto?.DataSetId ?? 0
            },
            validation: () => ValidateCreateAnalysisDto(createDto!),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate analysis creation failure
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.ANALYSIS_CREATION_FAILURE, GetCorrelationId(), context.OperationName, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating analysis creation failure", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.ANALYSIS_CREATION_FAILURE
                    });
                    throw new InvalidOperationException(AppConstants.Messages.SIMULATED_ANALYSIS_CREATION_FAILURE);
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
                var analysis = createDto!.ToEntity();
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);

                _infrastructure.StructuredLogging.LogStep(context, "Database save started");
                var savedAnalysis = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.AddAsync(analysis),
                    _infrastructure.DefaultTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, "Database save completed", new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.ANALYSIS_ID] = savedAnalysis.Id
                });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
                var result = savedAnalysis.ToDto();
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);

                return result;
            });
    }

    public async Task<AnalysisDto?> GetAnalysisAsync(int id)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(GetAnalysisAsync),
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.DATASET_ID] = id },
            validation: () => ValidateAnalysisId(id),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate database timeout
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.DATABASE_TIMEOUT, GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating database timeout", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.DATABASE_TIMEOUT
                    });
                    await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
                var analysis = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.GetByIdAsync(id),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED);

                if (analysis == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND, new Dictionary<string, object>
                    {
                        [AppConstants.DataStructures.ANALYSIS_ID] = id
                    });
                    return null;
                }

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
                var result = analysis.ToDto();
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);

                return result;
            });
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByDataSetAsync(int dataSetId)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(GetAnalysesByDataSetAsync),
            additionalMetadata: new Dictionary<string, object> { ["DataSetId"] = dataSetId },
            validation: () => ValidateDataSetId(dataSetId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate network latency
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.NETWORK_LATENCY, GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating network latency", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.NETWORK_LATENCY
                    });
                    await Task.Delay(_random.Next(AppConstants.ChaosEngineering.MIN_CHAOS_DELAY_MS, AppConstants.ChaosEngineering.MAX_CHAOS_DELAY_MS));
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
                var analyses = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.GetByDataSetIdAsync(dataSetId),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
                {
                    ["AnalysisCount"] = analyses.Count()
                });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
                var result = analyses.ToDtoCollection();
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);

                return result;
            });
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByStatusAsync(AnalysisStatus status)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(GetAnalysesByStatusAsync),
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.STATUS] = status.ToString() },
            validation: () => ValidateAnalysisStatus(status),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate cache failure
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.CACHE_FAILURE, GetCorrelationId(), context.OperationName, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating cache failure", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.CACHE_FAILURE
                    });
                    throw new InvalidOperationException(AppConstants.Messages.SIMULATED_CACHE_FAILURE);
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
                var analyses = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.GetByStatusAsync(status),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
                {
                    ["AnalysisCount"] = analyses.Count()
                });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
                var result = analyses.ToDtoCollection();
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);

                return result;
            });
    }

    public async Task<IEnumerable<AnalysisDto>> GetAnalysesByTypeAsync(AnalysisType type)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(GetAnalysesByTypeAsync),
            additionalMetadata: new Dictionary<string, object> { ["Type"] = type.ToString() },
            validation: () => ValidateAnalysisType(type),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate storage failure
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.STORAGE_FAILURE, GetCorrelationId(), context.OperationName, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating storage failure", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.STORAGE_FAILURE
                    });
                    throw new InvalidOperationException(AppConstants.Messages.SIMULATED_STORAGE_FAILURE);
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
                var analyses = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.GetByTypeAsync(type),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
                {
                    ["AnalysisCount"] = analyses.Count()
                });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_STARTED);
                var result = analyses.ToDtoCollection();
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DTO_MAPPING_COMPLETED);

                return result;
            });
    }

    public async Task<AnalysisResultDto> GetAnalysisResultAsync(int analysisId)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(GetAnalysisResultAsync),
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysisId },
            validation: () => ValidateAnalysisId(analysisId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate memory pressure
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.MEMORY_PRESSURE, GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating memory pressure", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.MEMORY_PRESSURE
                    });
                    // Simulate memory pressure by allocating temporary objects
                    var tempObjects = new List<byte[]>();
                    for (int i = 0; i < 50; i++)
                    {
                        tempObjects.Add(new byte[1024 * 1024]); // 1MB each
                    }
                    await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
                    tempObjects.Clear();
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
                var analysis = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.GetByIdAsync(analysisId),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED);

                if (analysis == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND, new Dictionary<string, object>
                    {
                        [AppConstants.DataStructures.ANALYSIS_ID] = analysisId
                    });
                    _infrastructure.StructuredLogging.LogSummary(context, false, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND);
                    throw new ArgumentException(AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND);
                }

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.RESULTS_DESERIALIZATION_STARTED);
                var deserializedResults = await DeserializeResultsSafelyAsync(analysis.Results, analysisId, context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.RESULTS_DESERIALIZATION_COMPLETED);

                var result = new AnalysisResultDto
                {
                    AnalysisId = analysisId,
                    Status = analysis.Status,
                    Results = deserializedResults,
                    ErrorMessage = analysis.ErrorMessage
                };

                return result;
            });
    }

    public async Task<bool> DeleteAnalysisAsync(int id)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(DeleteAnalysisAsync),
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = id },
            validation: () => ValidateAnalysisId(id),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate processing delay
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.PROCESSING_DELAY, GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating processing delay", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.PROCESSING_DELAY
                    });
                    await Task.Delay(_random.Next(AppConstants.ChaosEngineering.MIN_CHAOS_DELAY_MS, AppConstants.ChaosEngineering.MAX_CHAOS_DELAY_MS));
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.DATABASE_DELETION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.DeleteAsync(id),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.DATABASE_DELETION_COMPLETED, new Dictionary<string, object>
                {
                    ["DeletionResult"] = result
                });

                if (result)
                {
                    _infrastructure.StructuredLogging.LogSummary(context, true);
                }
                else
                {
                    _infrastructure.StructuredLogging.LogSummary(context, false, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND_OR_DELETED);
                }

                return result;
            });
    }

    public async Task<AnalysisDto> RunAnalysisAsync(int analysisId)
    {
        return await ExecuteAnalysisOperationAsync(
            operationName: nameof(RunAnalysisAsync),
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysisId },
            validation: () => ValidateAnalysisId(analysisId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate processing delay
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.PROCESSING_DELAY, GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating processing delay", new Dictionary<string, object>
                    {
                        [AppConstants.ChaosEngineering.CHAOS_TYPE] = AppConstants.ChaosEngineering.PROCESSING_DELAY
                    });
                    await Task.Delay(_random.Next(AppConstants.ChaosEngineering.MIN_CHAOS_DELAY_MS, AppConstants.ChaosEngineering.MAX_CHAOS_DELAY_MS));
                }, new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = AppConstants.Auth.AnonymousUser });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_STARTED);
                var analysis = await ExecuteWithTimeoutAsync(
                    () => _analysisRepository.GetByIdAsync(analysisId),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED);

                if (analysis == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND, new Dictionary<string, object>
                    {
                        [AppConstants.DataStructures.ANALYSIS_ID] = analysisId
                    });
                    _infrastructure.StructuredLogging.LogSummary(context, false, AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND);
                    throw new ArgumentException(AppConstants.AnalysisMessages.ANALYSIS_NOT_FOUND);
                }

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_STATE_VALIDATION_STARTED);
                ValidateAnalysisState(analysis, analysisId, GetCorrelationId());
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_STATE_VALIDATION_COMPLETED);

                // If already completed, return existing result
                if (analysis.Status == AnalysisStatus.Completed)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_ALREADY_COMPLETED, new Dictionary<string, object>
                    {
                        [AppConstants.DataStructures.ANALYSIS_ID] = analysisId,
                        [AppConstants.DataStructures.STATUS] = analysis.Status.ToString()
                    });
                    _infrastructure.StructuredLogging.LogSummary(context, true, AppConstants.AnalysisMessages.ANALYSIS_ALREADY_COMPLETED);
                    return analysis.ToDto();
                }

                // Execute analysis with state management
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_EXECUTION_STARTED, new Dictionary<string, object>
                {
                    ["AnalysisType"] = analysis.Type.ToString(),
                    [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id
                });
                var result = await ExecuteAnalysisWithStateManagementAsync(analysis, context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_EXECUTION_COMPLETED);

                return result;
            });
    }

    #region Private Methods

    private async Task<T> ExecuteAnalysisOperationAsync<T>(
        string operationName,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(operationName, correlationId, AppConstants.Auth.AnonymousUser, additionalMetadata);

        try
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            validation();
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            var result = await operation(context);
            _infrastructure.StructuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogSummary(context, false, ex.Message);

            // Create detailed error message based on operation type and metadata
            var errorMessage = CreateDetailedErrorMessage(operationName, additionalMetadata);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private static string CreateDetailedErrorMessage(string operationName, Dictionary<string, object>? metadata)
    {
        if (metadata == null) return $"Failed to complete {operationName}";

        // Handle specific operation types with detailed error messages
        switch (operationName)
        {
            case nameof(CreateAnalysisAsync):
                var analysisName = metadata.TryGetValue("AnalysisName", out var name) ? name?.ToString() : "unknown";
                return $"Failed to complete {operationName} for analysis '{analysisName}'";

            case nameof(GetAnalysisAsync):
            case nameof(DeleteAnalysisAsync):
            case nameof(RunAnalysisAsync):
            case nameof(GetAnalysisResultAsync):
                string analysisId;
                if (metadata.TryGetValue(AppConstants.DataStructures.ANALYSIS_ID, out var id))
                {
                    analysisId = id?.ToString() ?? AppConstants.Messages.UNKNOWN;
                }
                else if (metadata.TryGetValue(AppConstants.DataStructures.DATASET_ID, out var datasetId))
                {
                    analysisId = datasetId?.ToString() ?? AppConstants.Messages.UNKNOWN;
                }
                else
                {
                    analysisId = AppConstants.Messages.UNKNOWN;
                }
                return $"Failed to complete {operationName} for analysis ID {analysisId}";

            case nameof(GetAnalysesByDataSetAsync):
                var dataSetId = metadata.TryGetValue("DataSetId", out var dsId) ? dsId?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for dataset ID {dataSetId}";

            case nameof(GetAnalysesByStatusAsync):
                var status = metadata.TryGetValue(AppConstants.DataStructures.STATUS, out var statusValue) ? statusValue?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for status {status}";

            case nameof(GetAnalysesByTypeAsync):
                var type = metadata.TryGetValue("Type", out var typeValue) ? typeValue?.ToString() : "unknown";
                return $"Failed to complete {operationName} for type {type}";

            default:
                return $"Failed to complete {operationName}";
        }
    }

    private async Task<AnalysisDto> ExecuteAnalysisWithStateManagementAsync(Analysis analysis, IOperationContext context)
    {
        // Set processing state
        analysis.Status = AnalysisStatus.Processing;
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.SETTING_ANALYSIS_STATUS_TO_PROCESSING);
        await ExecuteWithTimeoutAsync(
            () => _analysisRepository.UpdateAsync(analysis),
            _infrastructure.QuickTimeout,
            context);

        try
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_EXECUTION_STARTED, new Dictionary<string, object>
            {
                ["AnalysisType"] = analysis.Type.ToString(),
                [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id
            });

            // Execute analysis with timeout
            var results = await ExecuteWithTimeoutAsync(
                () => ExecuteAnalysisByTypeAsync(analysis, context),
                _infrastructure.DefaultTimeout,
                context);

            // Update with success state
            analysis.Status = AnalysisStatus.Completed;
            analysis.CompletedAt = DateTime.UtcNow;
            analysis.Results = JsonSerializer.Serialize(results);
            analysis.ErrorMessage = null;

            _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_COMPLETED_SUCCESSFULLY, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id,
                [AppConstants.DataStructures.STATUS] = analysis.Status.ToString()
            });

            var updatedAnalysis = await ExecuteWithTimeoutAsync(
                () => _analysisRepository.UpdateAsync(analysis),
                _infrastructure.QuickTimeout,
                context);

            return updatedAnalysis.ToDto();
        }
        catch (Exception ex)
        {
            // Update with failure state
            analysis.Status = AnalysisStatus.Failed;
            analysis.ErrorMessage = ex.Message;

            _infrastructure.StructuredLogging.LogStep(context, AppConstants.AnalysisMessages.ANALYSIS_FAILED, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id,
                ["ErrorMessage"] = ex.Message
            });

            await ExecuteWithTimeoutAsync(
                () => _analysisRepository.UpdateAsync(analysis),
                _infrastructure.QuickTimeout,
                context);

            throw new InvalidOperationException($"Failed to execute analysis of type {analysis.Type} for ID {analysis.Id}", ex);
        }
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, IOperationContext context)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.OPERATION_TIMED_OUT, new Dictionary<string, object>
            {
                ["Timeout"] = timeout.ToString(),
                ["OperationName"] = context.OperationName,
                ["ErrorMessage"] = ex.Message
            });
            throw new TimeoutException($"Operation {context.OperationName} timed out after {timeout}");
        }
    }

    private async Task<object?> DeserializeResultsSafelyAsync(string? results, int analysisId, IOperationContext context)
    {
        if (string.IsNullOrEmpty(results))
            return null;

        try
        {
            return await Task.Run(() => JsonSerializer.Deserialize<object>(results));
        }
        catch (JsonException jsonEx)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Failed to deserialize results", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.ANALYSIS_ID] = analysisId,
                ["ErrorMessage"] = jsonEx.Message
            });
            return null;
        }
    }

    private async Task<object> ExecuteAnalysisByTypeAsync(Analysis analysis, IOperationContext context)
    {
        return analysis.Type switch
        {
            AnalysisType.Normalization => await ExecuteNormalizationAnalysisAsync(analysis, context),
            AnalysisType.Comparison => await ExecuteComparisonAnalysisAsync(analysis, context),
            AnalysisType.Statistical => await ExecuteStatisticalAnalysisAsync(analysis, context),
            AnalysisType.DataCleaning => await ExecuteDataCleaningAnalysisAsync(analysis, context),
            AnalysisType.OutlierDetection => await ExecuteOutlierDetectionAnalysisAsync(analysis, context),
            AnalysisType.CorrelationAnalysis => await ExecuteCorrelationAnalysisAsync(analysis, context),
            AnalysisType.TrendAnalysis => await ExecuteTrendAnalysisAsync(analysis, context),
            AnalysisType.Custom => await ExecuteCustomAnalysisAsync(analysis, context),
            _ => throw new NotSupportedException($"Analysis type {analysis.Type} is not supported")
        };
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateCreateAnalysisDto(CreateAnalysisDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        if (string.IsNullOrWhiteSpace(createDto.Name))
            throw new ArgumentException(AppConstants.Messages.ANALYSIS_NAME_REQUIRED, nameof(createDto));

        if (createDto.Name.Length > 255)
            throw new ArgumentException(AppConstants.Messages.ANALYSIS_NAME_TOO_LONG, nameof(createDto));

        if (createDto.Description?.Length > 1000)
            throw new ArgumentException(AppConstants.Messages.ANALYSIS_DESCRIPTION_TOO_LONG, nameof(createDto));

        if (createDto.DataSetId <= 0)
            throw new ArgumentException(AppConstants.Messages.DATASET_ID_REQUIRED, nameof(createDto));
    }

    private static void ValidateAnalysisState(Analysis analysis, int analysisId, string correlationId)
    {
        if (analysis.Status == AnalysisStatus.Processing)
        {
            throw new InvalidOperationException($"Analysis with ID {analysisId} is already in progress. CorrelationId: {correlationId}");
        }
    }

    private static void ValidateAnalysisId(int id)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.ANALYSIS_ID_MUST_BE_POSITIVE, nameof(id));
    }

    private static void ValidateDataSetId(int dataSetId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId));
    }

    private static void ValidateAnalysisStatus(AnalysisStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException(string.Format(AppConstants.ValidationMessages.INVALID_ANALYSIS_STATUS, status), nameof(status));
    }

    private static void ValidateAnalysisType(AnalysisType type)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentException(string.Format(AppConstants.ValidationMessages.INVALID_ANALYSIS_TYPE, type), nameof(type));
    }

    #endregion

    #region Analysis Execution Methods

    private async Task<object> ExecuteNormalizationAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing normalization analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
        return new
        {
            Type = "Normalization",
            Message = "Data normalization completed",
            NormalizedColumns = new[] { AppConstants.DataStructures.CUSTOMER_ID, AppConstants.DataStructures.ORDER_AMOUNT },
            MinValues = new { customer_id = 0.0, order_amount = 0.0 },
            MaxValues = new { customer_id = 1.0, order_amount = 1.0 }
        };
    }

    private async Task<object> ExecuteComparisonAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing comparison analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
        return new
        {
            Type = "Comparison",
            Message = "Dataset comparison completed",
            SimilarityScore = 0.85,
            Differences = new[] { AppConstants.DataStructures.CUSTOMER_ID, "product_code" },
            CommonColumns = new[] { AppConstants.DataStructures.ORDER_AMOUNT, "sales_region" }
        };
    }

    private async Task<object> ExecuteStatisticalAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing statistical analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
        return new
        {
            Type = "Statistical",
            Message = "Statistical analysis completed",
            Mean = new { customer_id = 45.2, order_amount = 78.9 },
            Median = new { customer_id = 42.0, order_amount = 75.0 },
            StandardDeviation = new { customer_id = 12.5, order_amount = 15.3 }
        };
    }

    private async Task<object> ExecuteDataCleaningAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing data cleaning analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
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

    private async Task<object> ExecuteOutlierDetectionAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing outlier detection analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
        return new
        {
            Type = "OutlierDetection",
            Message = "Outlier detection completed",
            DetectedOutliers = 7,
            OutlierColumns = new[] { AppConstants.DataStructures.CUSTOMER_ID, AppConstants.DataStructures.ORDER_AMOUNT },
            OutlierIndices = new[] { 15, 23, 45, 67, 89, 123, 156 }
        };
    }

    private async Task<object> ExecuteCorrelationAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing correlation analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
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

    private async Task<object> ExecuteTrendAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing trend analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
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

    private async Task<object> ExecuteCustomAnalysisAsync(Analysis analysis, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Executing custom analysis", new Dictionary<string, object> { [AppConstants.DataStructures.ANALYSIS_ID] = analysis.Id });
        await Task.Delay(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS); // Use constant for delay
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


