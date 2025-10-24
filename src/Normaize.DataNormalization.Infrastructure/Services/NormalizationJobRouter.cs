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
    private readonly ILogger<NormalizationJobRouter> _logger;

    public NormalizationJobRouter(
        IRemoveDuplicatesHandler removeDuplicatesHandler,
        ILogger<NormalizationJobRouter> logger)
    {
        _removeDuplicatesHandler = removeDuplicatesHandler ?? throw new ArgumentNullException(nameof(removeDuplicatesHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(NormalizationJob job, IJobProgress progress)
    {
        _logger.LogInformation("Routing job {JobId} with operation type {OperationType}", job.Id, job.OperationType);

        switch (job.OperationType.ToLowerInvariant())
        {
            case "removeduplicates":
                await _removeDuplicatesHandler.HandleAsync(job, progress);
                break;
            
            // TODO: Add more operation types as needed
            // case "normalizecase":
            //     await _normalizeCaseHandler.HandleAsync(job, progress);
            //     break;
            // case "standardizeformats":
            //     await _standardizeFormatsHandler.HandleAsync(job, progress);
            //     break;
            
            default:
                var errorMessage = $"Unsupported operation type: {job.OperationType}";
                _logger.LogError(errorMessage);
                await progress.FailedAsync(job.Id, errorMessage);
                throw new NotSupportedException(errorMessage);
        }
    }
}