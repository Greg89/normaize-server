using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Microsoft.Extensions.Logging;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Routes normalization jobs to appropriate handlers based on operation type
/// </summary>
public class NormalizationJobRouter : INormalizationJobRouter
{
    private readonly IRemoveDuplicatesHandler _removeDuplicatesHandler;
    private readonly IProcessDataSetFileHandler _processFileHandler;
    private readonly ILogger<NormalizationJobRouter> _logger;

    public NormalizationJobRouter(
        IRemoveDuplicatesHandler removeDuplicatesHandler,
        IProcessDataSetFileHandler processFileHandler,
        ILogger<NormalizationJobRouter> logger)
    {
        _removeDuplicatesHandler = removeDuplicatesHandler ?? throw new ArgumentNullException(nameof(removeDuplicatesHandler));
        _processFileHandler = processFileHandler ?? throw new ArgumentNullException(nameof(processFileHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(NormalizationJob job, IJobProgress progress)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (progress == null) throw new ArgumentNullException(nameof(progress));

        _logger.LogInformation("Routing job {JobId} with operation type {OperationType} for dataset {DataSetId}",
            job.Id, job.OperationType, job.DataSetId);

        try
        {
            switch (job.OperationType.ToLowerInvariant())
            {
                case "processfile":
                case "process_file":
                case "processdatasetfile":
                case "process_dataset_file":
                    await _processFileHandler.HandleAsync(job, progress);
                    break;

                case "removeduplicates":
                case "remove_duplicates":
                    await _removeDuplicatesHandler.HandleAsync(job, progress);
                    break;

                case "normalize_case":
                case "normalizecase":
                    await HandleNormalizeCaseAsync(job, progress);
                    break;

                case "standardize_format":
                case "standardizeformat":
                    await HandleStandardizeFormatAsync(job, progress);
                    break;

                case "validate_data":
                case "validatedata":
                    await HandleValidateDataAsync(job, progress);
                    break;

                default:
                    var errorMessage = $"Unsupported operation type: {job.OperationType}";
                    _logger.LogError(errorMessage);
                    await progress.FailedAsync(job.Id, errorMessage);
                    throw new NotSupportedException(errorMessage);
            }

            _logger.LogDebug("Successfully routed and processed job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing job {JobId} of type {OperationType}",
                job.Id, job.OperationType);
            throw;
        }
    }

    // Not-yet-implemented operation types - fail fast with a clear error
    private async Task HandleNormalizeCaseAsync(NormalizationJob job, IJobProgress progress)
    {
        const string errorMessage = "Normalize case operation is not yet implemented";
        _logger.LogWarning("Attempted to run unimplemented normalize_case operation for job {JobId}", job.Id);
        await progress.FailedAsync(job.Id, errorMessage);
        throw new NotSupportedException(errorMessage);
    }

    private async Task HandleStandardizeFormatAsync(NormalizationJob job, IJobProgress progress)
    {
        const string errorMessage = "Standardize format operation is not yet implemented";
        _logger.LogWarning("Attempted to run unimplemented standardize_format operation for job {JobId}", job.Id);
        await progress.FailedAsync(job.Id, errorMessage);
        throw new NotSupportedException(errorMessage);
    }

    private async Task HandleValidateDataAsync(NormalizationJob job, IJobProgress progress)
    {
        const string errorMessage = "Data validation operation is not yet implemented";
        _logger.LogWarning("Attempted to run unimplemented validate_data operation for job {JobId}", job.Id);
        await progress.FailedAsync(job.Id, errorMessage);
        throw new NotSupportedException(errorMessage);
    }
}