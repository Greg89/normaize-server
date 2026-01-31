using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Handler for processing dataset files asynchronously
/// Extracts schema, preview data, and row/column counts from uploaded files
/// </summary>
public class ProcessDataSetFileHandler : IProcessDataSetFileHandler
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly ILogger<ProcessDataSetFileHandler> _logger;

    public ProcessDataSetFileHandler(
        IDataSetRepository dataSetRepository,
        IFileProcessingService fileProcessingService,
        ILogger<ProcessDataSetFileHandler> logger)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(NormalizationJob job, IJobProgress progress)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (progress == null) throw new ArgumentNullException(nameof(progress));

        try
        {
            _logger.LogInformation("Processing file for dataset {DataSetId} (Job {JobId})",
                job.DataSetId, job.Id);

            // Get the dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(job.DataSetId);
            if (dataSet == null)
            {
                var error = $"Dataset {job.DataSetId} not found";
                _logger.LogError(error);
                await progress.FailedAsync(job.Id, error);
                return;
            }

            await progress.ReportAsync(job.Id, 10, "Loading file from storage...");

            // Process the file to extract schema and preview
            var processingResult = await _fileProcessingService.ProcessFileAsync(
                dataSet.FileInfo.FilePath,
                dataSet.FileInfo.FileType);

            if (processingResult.IsSuccess)
            {
                await progress.ReportAsync(job.Id, 70, "Updating dataset with schema and preview...");

                // Update the dataset with processing results
                dataSet.MarkAsProcessedWithDetails(
                    processingResult.Schema!,
                    processingResult.RowCount,
                    processingResult.ColumnCount,
                    processingResult.PreviewData);

                // Save the updated dataset
                await _dataSetRepository.UpdateAsync(dataSet);

                await progress.ReportAsync(job.Id, 100, "File processing completed successfully");

                await progress.SucceededAsync(job.Id, new
                {
                    RowCount = processingResult.RowCount,
                    ColumnCount = processingResult.ColumnCount,
                    HasSchema = !string.IsNullOrEmpty(processingResult.Schema),
                    HasPreview = !string.IsNullOrEmpty(processingResult.PreviewData)
                });

                _logger.LogInformation(
                    "Successfully processed file for dataset {DataSetId}: {RowCount} rows, {ColumnCount} columns",
                    job.DataSetId, processingResult.RowCount, processingResult.ColumnCount);
            }
            else
            {
                var error = processingResult.Error ?? "Unknown processing error";
                _logger.LogError("File processing failed for dataset {DataSetId}: {Error}",
                    job.DataSetId, error);

                // Mark dataset as failed
                dataSet.MarkProcessingAsFailed($"Error processing file: {error}");
                await _dataSetRepository.UpdateAsync(dataSet);

                await progress.FailedAsync(job.Id, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file for dataset {DataSetId} in job {JobId}",
                job.DataSetId, job.Id);

            // Try to mark dataset as failed
            try
            {
                var dataSet = await _dataSetRepository.GetByIdAsync(job.DataSetId);
                if (dataSet != null)
                {
                    dataSet.MarkProcessingAsFailed($"Error processing file: {ex.Message}");
                    await _dataSetRepository.UpdateAsync(dataSet);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update dataset {DataSetId} with error state",
                    job.DataSetId);
            }

            await progress.FailedAsync(job.Id, ex.Message);
            throw;
        }
    }
}
