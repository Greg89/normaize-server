using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Handler for RestoreDataSetCommand
/// </summary>
public class RestoreDataSetCommandHandler : IRequestHandler<RestoreDataSetCommand, RestoreDataSetResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<RestoreDataSetCommandHandler> _logger;

    public RestoreDataSetCommandHandler(
        IDataSetRepository dataSetRepository,
        IAuditService auditService,
        ILogger<RestoreDataSetCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<RestoreDataSetResult> Handle(RestoreDataSetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

            if (dataSet == null)
            {
                _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId);
                return new RestoreDataSetResult
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
                return new RestoreDataSetResult
                {
                    Success = false,
                    Error = $"Access denied to dataset {request.DataSetId}"
                };
            }

            // Check if dataset is already not deleted
            if (!dataSet.IsDeleted)
            {
                _logger.LogInformation("Dataset {DataSetId} is not deleted, no restore action needed", dataSet.Id);
                return new RestoreDataSetResult
                {
                    Success = true,
                    Message = "Dataset is not deleted, no restore action needed",
                    DataSetId = request.DataSetId
                };
            }

            // Restore dataset
            dataSet.Restore(request.UserId);

            // Save changes
            await _dataSetRepository.UpdateAsync(dataSet, cancellationToken);

            // Log audit action
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "RestoreDataSet",
                new Dictionary<string, object>(),
                cancellationToken);

            _logger.LogInformation("Successfully restored dataset {DataSetId}", dataSet.Id);

            return new RestoreDataSetResult
            {
                Success = true,
                Message = "Dataset restored successfully",
                DataSetId = request.DataSetId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore dataset {DataSetId}", request.DataSetId);
            return new RestoreDataSetResult
            {
                Success = false,
                Error = $"Failed to restore dataset: {ex.Message}"
            };
        }
    }
}
