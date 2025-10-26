using MediatR;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Handler for updating dataset retention policy
/// </summary>
public class UpdateRetentionPolicyCommandHandler : IRequestHandler<UpdateRetentionPolicyCommand, UpdateRetentionPolicyResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IAuditService _auditService;

    public UpdateRetentionPolicyCommandHandler(
        IDataSetRepository dataSetRepository,
        IAuditService auditService)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<UpdateRetentionPolicyResult> Handle(UpdateRetentionPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

            if (dataSet == null)
            {
                return new UpdateRetentionPolicyResult(
                    false,
                    "Dataset not found",
                    null,
                    "Dataset does not exist");
            }

            // Ensure user access
            dataSet.EnsureUserAccess(request.UserId);

            // Update retention policy
            dataSet.UpdateRetentionPolicy(request.RetentionDays, request.UserId);

            // Save changes
            await _dataSetRepository.UpdateAsync(dataSet, cancellationToken);

            // Log audit action
            await _auditService.LogDataSetActionAsync(
                dataSet.Id,
                request.UserId,
                "UpdateRetentionPolicy",
                new Dictionary<string, object>
                {
                    ["RetentionDays"] = request.RetentionDays,
                    ["NewExpiryDate"] = dataSet.RetentionExpiryDate!
                },
                cancellationToken);

            return new UpdateRetentionPolicyResult(
                true,
                "Retention policy updated successfully",
                dataSet.RetentionExpiryDate);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new UpdateRetentionPolicyResult(
                false,
                "Access denied",
                null,
                ex.Message);
        }
        catch (Exception ex)
        {
            return new UpdateRetentionPolicyResult(
                false,
                "Failed to update retention policy",
                null,
                ex.Message);
        }
    }
}
