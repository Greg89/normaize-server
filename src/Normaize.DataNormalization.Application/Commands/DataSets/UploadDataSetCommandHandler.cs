using MediatR;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

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

    public UploadDataSetCommandHandler(
        IDataSetRepository dataSetRepository,
        IFileStorageService fileStorageService,
        IFileProcessingService fileProcessingService,
        IAuditService auditService)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
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

            // Process the file to extract schema and preview
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
                    ["FileSize"] = request.FileSize
                },
                cancellationToken);

            return new UploadDataSetResult(
                true,
                "Dataset uploaded successfully",
                savedDataSet.Id);
        }
        catch (Exception ex)
        {
            return new UploadDataSetResult(
                false,
                "Upload failed",
                null,
                ex.Message);
        }
    }
}
