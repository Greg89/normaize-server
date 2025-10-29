using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Handler for soft deleting a dataset
/// </summary>
public class DeleteDataSetCommandHandler : IRequestHandler<DeleteDataSetCommand, DeleteDataSetResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<DeleteDataSetCommandHandler> _logger;

    public DeleteDataSetCommandHandler(
        IDataSetRepository dataSetRepository,
        IAuditService auditService,
        ILogger<DeleteDataSetCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeleteDataSetResult> Handle(DeleteDataSetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Deleting dataset {DataSetId} for user {UserId}",
                request.DataSetId,
                request.UserId);

            // Retrieve dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);
            if (dataSet == null)
            {
                return new DeleteDataSetResult(false, "Dataset not found", "DATASET_NOT_FOUND");
            }

            // Ensure user access
            dataSet.EnsureUserAccess(request.UserId);

            // Soft delete using domain method
            dataSet.Delete(request.DeletedBy);

            // Save changes
            await _dataSetRepository.UpdateAsync(dataSet, cancellationToken);

            // Audit log
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "Deleted",
                new Dictionary<string, object>
                {
                    { "DeletedBy", request.DeletedBy }
                },
                cancellationToken);

            _logger.LogInformation(
                "Successfully deleted dataset {DataSetId}",
                request.DataSetId);

            return new DeleteDataSetResult(true, "Dataset deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting dataset {DataSetId}",
                request.DataSetId);
            return new DeleteDataSetResult(false, $"Failed to delete dataset: {ex.Message}", "DELETE_FAILED");
        }
    }
}
