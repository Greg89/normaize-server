using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Handler for updating dataset metadata
/// </summary>
public class UpdateDataSetCommandHandler : IRequestHandler<UpdateDataSetCommand, UpdateDataSetResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateDataSetCommandHandler> _logger;

    public UpdateDataSetCommandHandler(
        IDataSetRepository dataSetRepository,
        IAuditService auditService,
        ILogger<UpdateDataSetCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateDataSetResult> Handle(UpdateDataSetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Updating dataset {DataSetId} for user {UserId}",
                request.DataSetId,
                request.UserId);

            // Retrieve dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);
            if (dataSet == null)
            {
                return new UpdateDataSetResult(false, "Dataset not found", "DATASET_NOT_FOUND");
            }

            // Ensure user access
            dataSet.EnsureUserAccess(request.UserId);

            // Update metadata using domain method
            dataSet.UpdateMetadata(
                request.Name,
                request.Description,
                request.ModifiedBy);

            // Save changes
            await _dataSetRepository.UpdateAsync(dataSet, cancellationToken);

            // Audit log
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "Updated",
                new Dictionary<string, object>
                {
                    { "Name", request.Name },
                    { "Description", request.Description ?? string.Empty }
                },
                cancellationToken);

            _logger.LogInformation(
                "Successfully updated dataset {DataSetId}",
                request.DataSetId);

            return new UpdateDataSetResult(true, "Dataset updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating dataset {DataSetId}",
                request.DataSetId);
            return new UpdateDataSetResult(false, $"Failed to update dataset: {ex.Message}", "UPDATE_FAILED");
        }
    }
}
