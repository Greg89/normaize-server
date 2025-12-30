using System;
using System.Linq;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Normaize.DataNormalization.Infrastructure.Handlers;

/// <summary>
/// Handler for duplicate row removal operations
/// </summary>
public class RemoveDuplicatesHandler : IRemoveDuplicatesHandler
{
    private readonly IDataSetDataLoader _dataLoader;
    private readonly IDataSetDataPersister _dataPersister;
    private readonly IDuplicateRemovalProcessor _duplicateProcessor;
    private readonly ILogger<RemoveDuplicatesHandler> _logger;

    public RemoveDuplicatesHandler(
        IDataSetDataLoader dataLoader,
        IDataSetDataPersister dataPersister,
        IDuplicateRemovalProcessor duplicateProcessor,
        ILogger<RemoveDuplicatesHandler> logger)
    {
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _dataPersister = dataPersister ?? throw new ArgumentNullException(nameof(dataPersister));
        _duplicateProcessor = duplicateProcessor ?? throw new ArgumentNullException(nameof(duplicateProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(NormalizationJob job, IJobProgress progress)
    {
        try
        {
            _logger.LogInformation("Starting duplicate removal for job {JobId}, dataset {DataSetId}", job.Id, job.DataSetId);
            await progress.StartedAsync(job.Id);

            // Parse the operation parameters to get the duplicate removal options
            var options = DuplicateRemovalOptions.Deserialize(job.OperationParameters);

            await progress.ReportAsync(job.Id, 5, "Loading dataset data");

            // Load the dataset data
            var dataSetData = await _dataLoader.LoadDataSetDataAsync(job.DataSetId);
            _logger.LogInformation("Loaded dataset with {RowCount} rows and {ColumnCount} columns",
                dataSetData.TotalRows, dataSetData.TotalColumns);

            await progress.ReportAsync(job.Id, 10, "Validating duplicate removal options");

            // Validate that the specified key columns exist
            ValidateKeyColumns(dataSetData, options);

            // Create a backup before processing
            await progress.ReportAsync(job.Id, 15, "Creating backup of original data");
            var backupId = await _dataPersister.CreateBackupAsync(job.DataSetId);
            _logger.LogInformation("Created backup {BackupId} for dataset {DataSetId}", backupId, job.DataSetId);

            // Set up progress reporting
            var duplicateProgress = new Progress<DuplicateRemovalProgress>(dupProgress =>
            {
                // Map duplicate removal progress to job progress (15% to 85%)
                var jobProgressPercent = 15 + (int)(dupProgress.PercentComplete * 0.7);
                progress.ReportAsync(job.Id, jobProgressPercent, dupProgress.CurrentOperation).Wait();
            });

            // Process duplicates
            await progress.ReportAsync(job.Id, 15, "Starting duplicate detection and removal");
            var result = await _duplicateProcessor.RemoveDuplicatesAsync(dataSetData, options, duplicateProgress);

            // Save the processed data
            await progress.ReportAsync(job.Id, 90, "Saving processed data");
            var saveSuccess = await _dataPersister.SaveProcessedDataAsync(job.DataSetId, result.ProcessedData, "RemoveDuplicates");

            if (!saveSuccess)
            {
                throw new InvalidOperationException("Failed to save processed data");
            }

            // Create result summary
            var jobResult = new
            {
                OriginalRowCount = result.OriginalRowCount,
                DuplicatesRemoved = result.DuplicatesRemoved,
                FinalRowCount = result.FinalRowCount,
                KeyColumns = result.ProcessedColumns,
                RetentionStrategy = result.RetentionStrategy.ToString(),
                CaseSensitivity = result.CaseSensitivity.ToString(),
                ProcessingTimeMs = (int)result.ProcessingTime.TotalMilliseconds,
                BackupId = backupId
            };

            await progress.SucceededAsync(job.Id, jobResult);
            _logger.LogInformation("Duplicate removal completed for job {JobId}. Removed {DuplicatesRemoved} duplicates from {OriginalCount} rows",
                job.Id, result.DuplicatesRemoved, result.OriginalRowCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Duplicate removal failed for job {JobId}", job.Id);
            await progress.FailedAsync(job.Id, ex.Message);
            throw;
        }
    }

    private static void ValidateKeyColumns(DataSetData dataSetData, DuplicateRemovalOptions options)
    {
        var availableColumns = dataSetData.Columns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingColumns = options.KeyColumns.Where(col => !availableColumns.Contains(col)).ToList();

        if (missingColumns.Any())
        {
            throw new ArgumentException($"The following key columns were not found in the dataset: {string.Join(", ", missingColumns)}. Available columns: {string.Join(", ", availableColumns)}");
        }
    }
}