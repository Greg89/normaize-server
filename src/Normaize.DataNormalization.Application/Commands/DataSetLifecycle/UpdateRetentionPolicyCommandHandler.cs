using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Handler for UpdateRetentionPolicyCommand
/// </summary>
public class UpdateRetentionPolicyCommandHandler : IRequestHandler<UpdateRetentionPolicyCommand, UpdateRetentionPolicyResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateRetentionPolicyCommandHandler> _logger;

    public UpdateRetentionPolicyCommandHandler(
        IDataSetRepository dataSetRepository,
        IAuditService auditService,
        ILogger<UpdateRetentionPolicyCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<UpdateRetentionPolicyResult> Handle(UpdateRetentionPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

            if (dataSet == null)
            {
                _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId);
                return new UpdateRetentionPolicyResult
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
                return new UpdateRetentionPolicyResult
                {
                    Success = false,
                    Error = $"Access denied to dataset {request.DataSetId}"
                };
            }

            // Store old expiry date for audit
            var oldExpiryDate = dataSet.RetentionExpiryDate;

            // Update retention policy
            dataSet.UpdateRetentionPolicy(request.RetentionDays, request.UserId);

            // Calculate new expiry date
            var newExpiryDate = dataSet.RetentionExpiryDate ?? DateTime.UtcNow.AddDays(request.RetentionDays);

            // Save changes
            await _dataSetRepository.UpdateAsync(dataSet, cancellationToken);

            // Log audit action
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "UpdateRetentionPolicy",
                new Dictionary<string, object>
                {
                    ["OldExpiryDate"] = oldExpiryDate?.ToString() ?? "null",
                    ["NewExpiryDate"] = newExpiryDate.ToString(),
                    ["RetentionDays"] = request.RetentionDays
                },
                cancellationToken);

            _logger.LogInformation("Successfully updated retention policy for dataset {DataSetId} to {RetentionDays} days",
                dataSet.Id, request.RetentionDays);

            return new UpdateRetentionPolicyResult
            {
                Success = true,
                Message = $"Retention policy updated successfully to {request.RetentionDays} days",
                DataSetId = request.DataSetId,
                RetentionDays = request.RetentionDays,
                ExpiryDate = newExpiryDate,
                IsExpired = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update retention policy for dataset {DataSetId}", request.DataSetId);
            return new UpdateRetentionPolicyResult
            {
                Success = false,
                Error = $"Failed to update retention policy: {ex.Message}"
            };
        }
    }
}
