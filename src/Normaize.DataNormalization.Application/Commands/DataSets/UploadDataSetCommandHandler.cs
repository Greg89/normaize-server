using MediatR;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Handler for uploading dataset files
/// </summary>
public class UploadDataSetCommandHandler : IRequestHandler<UploadDataSetCommand, UploadDataSetResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IAuditService _auditService;
    private readonly INormalizationJobRepository _jobRepository;
    private readonly IJobQueue _jobQueue;

    // Threshold for sync vs async processing: 5MB or 1000 rows (estimated)
    private const long AsyncProcessingFileSizeThreshold = 5 * 1024 * 1024; // 5MB

    public UploadDataSetCommandHandler(
        IDataSetRepository dataSetRepository,
        IFileStorageService fileStorageService,
        IFileProcessingService fileProcessingService,
        IAuditService auditService,
        INormalizationJobRepository jobRepository,
        IJobQueue jobQueue)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
    }

    public async Task<UploadDataSetResult> Handle(UploadDataSetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate file
            var validationResult = await _fileProcessingService.ValidateFileAsync(
                request.FileStream,
                request.FileName,
                request.FileSize,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                return new UploadDataSetResult(
                    false,
                    "File validation failed",
                    null,
                    null,
                    validationResult.Error);
            }

            // Save file to storage
            var savedFilePath = await _fileStorageService.SaveFileAsync(
                request.FileStream,
                request.FileName,
                request.UserId,
                cancellationToken);

            // Create file metadata
            var fileMetadata = FileMetadata.CreateFromFileName(
                request.FileName,
                savedFilePath,
                request.FileSize);

            // Create dataset aggregate
            var dataSet = DataSet.Create(
                request.Name,
                request.Description,
                request.UserId,
                fileMetadata,
                statistics: null,
                retentionDays: request.RetentionDays);

            // Decide: sync or async processing based on file size
            bool processAsync = request.FileSize >= AsyncProcessingFileSizeThreshold;
            Guid? processingJobId = null;

            if (processAsync)
            {
                // Large file: Queue for async processing
                // Save dataset first with Pending status
                var savedDataSet = await _dataSetRepository.AddAsync(dataSet, cancellationToken);

                // Create and queue processing job
                var processingJob = NormalizationJob.Create(
                    savedDataSet.Id,
                    "ProcessFile",
                    "{}"); // Empty parameters for now

                await _jobRepository.SaveAsync(processingJob);
                await _jobQueue.EnqueueAsync(processingJob);

                processingJobId = processingJob.Id;

                // Log audit action
                await _auditService.LogDataSetActionAsync(
                    savedDataSet.Id,
                    request.UserId,
                    "UploadDataSet",
                    new Dictionary<string, object>
                    {
                        ["FileName"] = request.FileName,
                        ["FileSize"] = request.FileSize,
                        ["ProcessingMode"] = "Async",
                        ["ProcessingJobId"] = processingJobId
                    },
                    cancellationToken);

                return new UploadDataSetResult(
                    true,
                    "Dataset uploaded successfully. Processing in background...",
                    savedDataSet.Id,
                    processingJobId);
            }
            else
            {
                // Small file: Process synchronously (existing behavior)
                var processingResult = await _fileProcessingService.ProcessFileAsync(
                    savedFilePath,
                    fileMetadata.FileType,
                    cancellationToken);

                if (processingResult.IsSuccess)
                {
                    dataSet.MarkAsProcessedWithDetails(
                        processingResult.Schema!,
                        processingResult.RowCount,
                        processingResult.ColumnCount,
                        processingResult.PreviewData);
                }
                else
                {
                    dataSet.MarkProcessingAsFailed(processingResult.Error ?? "Unknown processing error");
                }

                // Save to repository
                var savedDataSet = await _dataSetRepository.AddAsync(dataSet, cancellationToken);

                // Log audit action
                await _auditService.LogDataSetActionAsync(
                    savedDataSet.Id,
                    request.UserId,
                    "UploadDataSet",
                    new Dictionary<string, object>
                    {
                        ["FileName"] = request.FileName,
                        ["FileSize"] = request.FileSize,
                        ["ProcessingMode"] = "Sync"
                    },
                    cancellationToken);

                return new UploadDataSetResult(
                    true,
                    "Dataset uploaded and processed successfully",
                    savedDataSet.Id);
            }
        }
        catch (Exception ex)
        {
            return new UploadDataSetResult(
                false,
                "Upload failed",
                null,
                null,
                ex.Message);
        }
    }
}
