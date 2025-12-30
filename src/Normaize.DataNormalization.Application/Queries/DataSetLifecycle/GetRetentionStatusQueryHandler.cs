using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Queries.DataSetLifecycle;

/// <summary>
/// Handler for GetRetentionStatusQuery
/// </summary>
public class GetRetentionStatusQueryHandler : IRequestHandler<GetRetentionStatusQuery, GetRetentionStatusResult>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly ILogger<GetRetentionStatusQueryHandler> _logger;

    public GetRetentionStatusQueryHandler(
        IDataSetRepository dataSetRepository,
        ILogger<GetRetentionStatusQueryHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _logger = logger;
    }

    public async Task<GetRetentionStatusResult> Handle(GetRetentionStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

            if (dataSet == null)
            {
                _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId);
                return new GetRetentionStatusResult
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
                return new GetRetentionStatusResult
                {
                    Success = false,
                    Error = $"Access denied to dataset {request.DataSetId}"
                };
            }

            // Calculate retention status
            var isExpired = dataSet.IsRetentionExpired;
            var expiryDate = dataSet.RetentionExpiryDate;

            var daysRemaining = expiryDate.HasValue && !isExpired
                ? (int)(expiryDate.Value - DateTime.UtcNow).TotalDays
                : 0;

            var retentionDays = expiryDate.HasValue
                ? (int)(expiryDate.Value - dataSet.UploadedAt).TotalDays
                : (int?)null;

            _logger.LogInformation("Retrieved retention status for dataset {DataSetId}: Expired={IsExpired}, DaysRemaining={DaysRemaining}",
                dataSet.Id, isExpired, daysRemaining);

            return new GetRetentionStatusResult
            {
                Success = true,
                DataSetId = request.DataSetId,
                RetentionDays = retentionDays,
                RetentionExpiryDate = expiryDate,
                IsRetentionExpired = isExpired,
                DaysUntilExpiry = daysRemaining
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retention status for dataset {DataSetId}", request.DataSetId);
            return new GetRetentionStatusResult
            {
                Success = false,
                Error = $"Failed to get retention status: {ex.Message}"
            };
        }
    }
}
