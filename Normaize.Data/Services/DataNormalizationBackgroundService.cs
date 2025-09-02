using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Services.DataNormalization;
using System.Text.Json;

namespace Normaize.Data.Services;

/// <summary>
/// Background service for processing data normalization jobs
/// </summary>
public class DataNormalizationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataNormalizationBackgroundService> _logger;
    private readonly DataNormalizationBackgroundServiceOptions _options;

    public DataNormalizationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DataNormalizationBackgroundService> logger,
        IOptions<DataNormalizationBackgroundServiceOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data normalization background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var jobQueueService = scope.ServiceProvider.GetRequiredService<IJobQueueService>();

                // Dequeue next job
                var job = await jobQueueService.DequeueJobAsync(stoppingToken);

                if (job != null)
                {
                    _logger.LogInformation("Processing normalization job {JobId} for dataset {DataSetId}",
                        job.Id, job.DataSetId);

                    // Process the job
                    await ProcessJobAsync(job, jobQueueService, stoppingToken);
                }
                else
                {
                    // No jobs available, wait before checking again
                    await Task.Delay(_options.IdleDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data normalization background service");

                // Wait before retrying
                await Task.Delay(_options.ErrorRetryDelay, stoppingToken);
            }
        }

        _logger.LogInformation("Data normalization background service stopped");
    }

    private async Task ProcessJobAsync(DataNormalizationJob job, IJobQueueService jobQueueService, CancellationToken cancellationToken)
    {
        try
        {
            // Mark job as started
            await jobQueueService.MarkJobAsStartedAsync(job.Id);

            // Create progress callback
            var progress = new Progress<int>(async (percentage) =>
            {
                try
                {
                    await jobQueueService.UpdateJobProgressAsync(job.Id, percentage, GetProgressMessage(percentage));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update progress for job {JobId}", job.Id);
                }
            });

            // Process based on operation type
            switch (job.OperationType)
            {
                case DataNormalizationConstants.DataNormalization.REMOVE_DUPLICATE_ROWS:
                    await ProcessDuplicateRowRemovalAsync(job, progress, jobQueueService, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"Operation type '{job.OperationType}' is not supported");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process job {JobId}", job.Id);

            // Mark job as failed
            await jobQueueService.MarkJobAsFailedAsync(job.Id, ex.Message);

            // Schedule retry if appropriate
            if (job.RetryCount < job.MaxRetries)
            {
                var nextRetryAt = DateTime.UtcNow.AddMinutes(CalculateRetryDelay(job.RetryCount));
                await jobQueueService.RetryJobAsync(job.Id, nextRetryAt);
            }
        }
    }

    private async Task ProcessDuplicateRowRemovalAsync(
        DataNormalizationJob job,
        IProgress<int> progress,
        IJobQueueService jobQueueService,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var duplicateRowRemovalProcessor = scope.ServiceProvider.GetRequiredService<IDuplicateRowRemovalProcessor>();
        var dataSetRepository = scope.ServiceProvider.GetRequiredService<IDataSetRepository>();

        // Get the dataset
        var dataSet = await dataSetRepository.GetByIdAsync(job.DataSetId);
        if (dataSet == null)
        {
            throw new InvalidOperationException($"Dataset {job.DataSetId} not found");
        }

        // Parse operation parameters
        var request = JsonSerializer.Deserialize<RemoveDuplicateRowsRequest>(job.OperationParameters!);
        if (request == null)
        {
            throw new InvalidOperationException("Invalid operation parameters");
        }

        // Process the dataset
        var results = await duplicateRowRemovalProcessor.ProcessAsync(
            dataSet,
            request,
            progress,
            cancellationToken);

        // Mark job as completed
        var resultsJson = JsonSerializer.Serialize(results);
        await jobQueueService.MarkJobAsCompletedAsync(job.Id, resultsJson);

        _logger.LogInformation("Successfully completed duplicate row removal job {JobId}. Removed {DuplicateCount} duplicates, {RemainingCount} rows remaining",
            job.Id, results.DuplicateRowsRemoved, results.RowsRemaining);
    }

    private static string GetProgressMessage(int percentage)
    {
        return percentage switch
        {
            < 25 => DataNormalizationConstants.DataNormalization.ANALYZING_DATASET,
            < 50 => DataNormalizationConstants.DataNormalization.PROCESSING_ROWS,
            < 75 => DataNormalizationConstants.DataNormalization.REMOVING_DUPLICATES,
            < 95 => DataNormalizationConstants.DataNormalization.UPDATING_DATASET,
            < 100 => DataNormalizationConstants.DataNormalization.VALIDATING_RESULTS,
            100 => "Processing completed",
            _ => "Processing in progress"
        };
    }

    private static int CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff with jitter
        var baseDelay = Math.Pow(2, retryCount) * 5; // 5, 10, 20, 40 minutes
        var jitter = new Random().Next(-2, 3); // ±2 minutes
        return Math.Min((int)baseDelay + jitter, 60); // Cap at 60 minutes
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping data normalization background service");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Configuration options for the data normalization background service
/// </summary>
public class DataNormalizationBackgroundServiceOptions
{
    /// <summary>
    /// How long to wait when no jobs are available
    /// </summary>
    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long to wait after an error before retrying
    /// </summary>
    public TimeSpan ErrorRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of concurrent job processors
    /// </summary>
    public int MaxConcurrentProcessors { get; set; } = 3;
}
