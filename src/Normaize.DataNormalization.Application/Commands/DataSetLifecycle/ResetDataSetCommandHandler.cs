using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Handler for ResetDataSetCommand
/// </summary>
public class ResetDataSetCommandHandler : IRequestHandler<ResetDataSetCommand, ResetDataSetResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ResetDataSetCommandHandler> _logger;

    public ResetDataSetCommandHandler(
        IDataSetRepository dataSetRepository,
        IFileStorageService fileStorageService,
        IFileProcessingService fileProcessingService,
        IAuditService auditService,
        ILogger<ResetDataSetCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _fileStorageService = fileStorageService;
        _fileProcessingService = fileProcessingService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ResetDataSetResult> Handle(ResetDataSetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve dataset (allow retrieval of deleted datasets for restore)
            var dataSet = request.ResetType == ResetType.Restore
                ? await _dataSetRepository.GetByIdIncludingDeletedAsync(request.DataSetId, cancellationToken)
                : await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

            if (dataSet == null)
            {
                _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId);
                return new ResetDataSetResult
                {
                    Success = false,
                    Error = $"Dataset with ID {request.DataSetId} not found"
                };
            }

            // Verify user access
            try
            {
                dataSet.EnsureUserAccess(request.UserId);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "User {UserId} does not have access to dataset {DataSetId}", request.UserId, request.DataSetId);
                return new ResetDataSetResult
                {
                    Success = false,
                    Error = $"Access denied to dataset {request.DataSetId}"
                };
            }

            // Perform reset based on type
            ResetDataSetResult result = request.ResetType switch
            {
                ResetType.Reprocess => await PerformReprocessResetAsync(dataSet, request, cancellationToken),
                ResetType.Restore => await PerformRestoreResetAsync(dataSet, request, cancellationToken),
                _ => new ResetDataSetResult
                {
                    Success = false,
                    Error = $"Unsupported reset type: {request.ResetType}"
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset dataset {DataSetId}", request.DataSetId);
            return new ResetDataSetResult
            {
                Success = false,
                Error = $"Failed to reset dataset: {ex.Message}"
            };
        }
    }

    private async Task<ResetDataSetResult> PerformReprocessResetAsync(
        Domain.Entities.DataSet dataSet,
        ResetDataSetCommand request,
        CancellationToken cancellationToken)
    {
        // Check if original file is still available
        var fileExists = await _fileStorageService.FileExistsAsync(dataSet.FileInfo.FilePath, cancellationToken);

        if (!fileExists)
        {
            _logger.LogWarning("Original file not found for dataset {DataSetId} at path {FilePath}",
                dataSet.Id, dataSet.FileInfo.FilePath);

            return new ResetDataSetResult
            {
                Success = false,
                Message = "Cannot reset dataset: Original file no longer exists in storage",
                DataSetId = request.DataSetId,
                FileAvailable = false,
                ErrorCode = "FILE_NOT_FOUND",
                Error = "Original file no longer exists in storage"
            };
        }

        try
        {
            // If dataset was deleted, restore it first
            if (dataSet.IsDeleted)
            {
                dataSet.Restore(request.UserId);
            }

            // Reset dataset to original state
            dataSet.ResetToOriginal(request.UserId);

            // Reprocess the original file
            var processingResult = await _fileProcessingService.ProcessFileAsync(
                dataSet.FileInfo.FilePath,
                dataSet.FileInfo.FileType,
                cancellationToken);

            if (!processingResult.IsSuccess)
            {
                _logger.LogError("Failed to reprocess file for dataset {DataSetId}: {Error}",
                    dataSet.Id, processingResult.Error);

                return new ResetDataSetResult
                {
                    Success = false,
                    Message = "Failed to reprocess file",
                    DataSetId = request.DataSetId,
                    FileAvailable = true,
                    Error = processingResult.Error
                };
            }

            // Update dataset with reprocessed data
            dataSet.MarkAsProcessedWithDetails(
                processingResult.Schema ?? string.Empty,
                processingResult.RowCount,
                processingResult.ColumnCount,
                processingResult.PreviewData,
                request.UserId);

            // Save changes
            await _dataSetRepository.UpdateAsync(dataSet, cancellationToken);

            // Log audit action
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "ResetDataSet_FileBased",
                new Dictionary<string, object>
                {
                    ["ResetType"] = "FileBased",
                    ["FilePath"] = dataSet.FileInfo.FilePath,
                    ["Reason"] = request.Reason ?? "No reason provided"
                },
                cancellationToken);

            _logger.LogInformation("Successfully reset and reprocessed dataset {DataSetId}", dataSet.Id);

            return new ResetDataSetResult
            {
                Success = true,
                Message = "Dataset reset successfully using original file",
                DataSetId = request.DataSetId,
                ResetType = "FileBased",
                FileAvailable = true,
                Reprocessed = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset dataset {DataSetId}", dataSet.Id);
            return new ResetDataSetResult
            {
                Success = false,
                Message = "Failed to reset dataset",
                DataSetId = request.DataSetId,
                FileAvailable = true,
                Error = ex.Message
            };
        }
    }

    private async Task<ResetDataSetResult> PerformRestoreResetAsync(
        Domain.Entities.DataSet dataSet,
        ResetDataSetCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // If dataset was deleted, restore it
            if (dataSet.IsDeleted)
            {
                dataSet.Restore(request.UserId);
            }

            // Save changes
            await _dataSetRepository.UpdateAsync(dataSet, cancellationToken);

            // Log audit action
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "ResetDataSet_DatabaseOnly",
                new Dictionary<string, object>
                {
                    ["ResetType"] = "DatabaseOnly",
                    ["Reason"] = request.Reason ?? "No reason provided"
                },
                cancellationToken);

            _logger.LogInformation("Successfully restored dataset {DataSetId}", dataSet.Id);

            return new ResetDataSetResult
            {
                Success = true,
                Message = "Dataset restored successfully (database only)",
                DataSetId = request.DataSetId,
                ResetType = "DatabaseOnly"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore dataset {DataSetId}", dataSet.Id);
            return new ResetDataSetResult
            {
                Success = false,
                Message = "Failed to restore dataset",
                DataSetId = request.DataSetId,
                Error = ex.Message
            };
        }
    }
}
