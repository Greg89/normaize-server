using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Handler for HardDeleteDataSetCommand
/// </summary>
public class HardDeleteDataSetCommandHandler : IRequestHandler<HardDeleteDataSetCommand, HardDeleteDataSetResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditService _auditService;
    private readonly ILogger<HardDeleteDataSetCommandHandler> _logger;

    public HardDeleteDataSetCommandHandler(
        IDataSetRepository dataSetRepository,
        IFileStorageService fileStorageService,
        IAuditService auditService,
        ILogger<HardDeleteDataSetCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _fileStorageService = fileStorageService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<HardDeleteDataSetResult> Handle(HardDeleteDataSetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

            if (dataSet == null)
            {
                _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId);
                return new HardDeleteDataSetResult
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
                return new HardDeleteDataSetResult
                {
                    Success = false,
                    Error = $"Access denied to dataset {request.DataSetId}"
                };
            }

            var fileName = dataSet.FileInfo.FileName;
            var filePath = dataSet.FileInfo.FilePath;
            var fileDeleted = false;

            // Delete the file from storage
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(filePath, cancellationToken);
                    fileDeleted = true;
                    _logger.LogInformation("Deleted file {FilePath} for dataset {DataSetId}", filePath, dataSet.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FilePath} for dataset {DataSetId}, continuing with database deletion",
                        filePath, dataSet.Id);
                    // Continue with database deletion even if file deletion fails
                }
            }

            // Remove from database (permanent delete)
            var deleted = await _dataSetRepository.DeleteAsync(dataSet.Id, cancellationToken);

            if (!deleted)
            {
                _logger.LogError("Failed to delete dataset {DataSetId} from database", dataSet.Id);
                return new HardDeleteDataSetResult
                {
                    Success = false,
                    Error = "Failed to delete dataset from database"
                };
            }

            // Log audit action
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "HardDeleteDataSet",
                new Dictionary<string, object>
                {
                    ["FileName"] = fileName,
                    ["FilePath"] = filePath
                },
                cancellationToken);

            _logger.LogInformation("Successfully hard deleted dataset {DataSetId}", dataSet.Id);

            return new HardDeleteDataSetResult
            {
                Success = true,
                Message = "Dataset permanently deleted",
                DataSetId = request.DataSetId,
                FileDeleted = fileDeleted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hard delete dataset {DataSetId}", request.DataSetId);
            return new HardDeleteDataSetResult
            {
                Success = false,
                Error = $"Failed to hard delete dataset: {ex.Message}"
            };
        }
    }
}
