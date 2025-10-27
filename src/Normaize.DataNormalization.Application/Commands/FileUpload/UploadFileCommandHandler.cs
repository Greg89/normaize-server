using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Commands.FileUpload;

/// <summary>
/// Handler for uploading and processing files
/// </summary>
public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadFileResult>
{
    private readonly IFileValidationService _validationService;
    private readonly IFileStorageService _storageService;
    private readonly IFileProcessingService _processingService;
    private readonly ILogger<UploadFileCommandHandler> _logger;

    public UploadFileCommandHandler(
        IFileValidationService validationService,
        IFileStorageService storageService,
        IFileProcessingService processingService,
        ILogger<UploadFileCommandHandler> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UploadFileResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting file upload for user {UserId}: {FileName}", request.UserId, request.FileName);

        try
        {
            // Step 1: Validate the file
            var validationResult = await _validationService.ValidateFileAsync(
                request.FileName,
                request.FileSize,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("File validation failed for {FileName}: {Error}", request.FileName, validationResult.Error);
                return new UploadFileResult(false, Error: validationResult.Error);
            }

            // Step 2: Determine file type
            var extension = _validationService.GetFileExtension(request.FileName);
            var fileType = MapExtensionToFileType(extension);

            // Step 3: Save the file
            var filePath = await _storageService.SaveFileAsync(
                request.FileStream,
                request.FileName,
                request.UserId,
                cancellationToken);

            _logger.LogInformation("File saved successfully: {FilePath}", filePath);

            // Step 4: Process the file if requested
            FileProcessingResult? processingResult = null;
            if (request.ProcessImmediately)
            {
                _logger.LogDebug("Processing file immediately: {FilePath}", filePath);

                var processResult = await _processingService.ProcessFileAsync(
                    filePath,
                    fileType,
                    cancellationToken);

                processingResult = new FileProcessingResult(
                    processResult.IsSuccess,
                    processResult.Schema,
                    processResult.RowCount,
                    processResult.ColumnCount,
                    processResult.PreviewData,
                    processResult.Error
                );

                if (!processResult.IsSuccess)
                {
                    _logger.LogWarning("File processing failed: {Error}", processResult.Error);
                }
            }

            _logger.LogInformation("File upload completed successfully for {FileName}", request.FileName);

            return new UploadFileResult(
                Success: true,
                FilePath: filePath,
                FileId: Path.GetFileName(filePath),
                ProcessingResult: processingResult
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", request.FileName);
            return new UploadFileResult(false, Error: $"Upload failed: {ex.Message}");
        }
    }

    private static FileType MapExtensionToFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".csv" => FileType.CSV,
            ".json" => FileType.JSON,
            ".xml" => FileType.XML,
            ".xlsx" or ".xls" => FileType.Excel,
            ".txt" => FileType.TXT,
            _ => throw new NotSupportedException($"File type {extension} is not supported")
        };
    }
}
