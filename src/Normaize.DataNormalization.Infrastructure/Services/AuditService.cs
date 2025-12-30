using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Implementation of audit service for logging dataset actions
/// </summary>
public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task LogDataSetActionAsync(
        Guid dataSetId,
        string userId,
        string action,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Dataset Action: {Action} by User: {UserId} on Dataset: {DataSetId}. Metadata: {@Metadata}",
            action,
            userId,
            dataSetId,
            metadata);

        // In production, this would write to a dedicated audit log table or service
        // For now, we're using structured logging

        return Task.CompletedTask;
    }
}
