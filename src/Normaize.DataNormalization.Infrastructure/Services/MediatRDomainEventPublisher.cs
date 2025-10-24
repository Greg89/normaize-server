using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediatR;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Events;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// MediatR-based implementation of domain event publisher
/// </summary>
public class MediatRDomainEventPublisher : IDomainEventPublisher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRDomainEventPublisher> _logger;

    public MediatRDomainEventPublisher(IMediator mediator, ILogger<MediatRDomainEventPublisher> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(IDomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));

        try
        {
            _logger.LogDebug("Publishing domain event: {EventType} at {OccurredAt}", 
                domainEvent.GetType().Name, domainEvent.OccurredAt);

            // Convert domain event to MediatR notification
            var notification = CreateNotificationWrapper(domainEvent);
            if (notification != null)
            {
                await _mediator.Publish(notification);
                _logger.LogDebug("Successfully published domain event: {EventType}", 
                    domainEvent.GetType().Name);
            }
            else
            {
                _logger.LogWarning("No MediatR wrapper found for domain event: {EventType}", 
                    domainEvent.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event: {EventType}", 
                domainEvent.GetType().Name);
            throw;
        }
    }

    private static INotification? CreateNotificationWrapper(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            JobCreated e => new JobCreatedNotification(e.JobId, e.DataSetId, e.OperationType, e.OccurredAt),
            JobStarted e => new JobStartedNotification(e.JobId, e.DataSetId, e.OperationType, e.OccurredAt),
            JobProgressUpdated e => new JobProgressUpdatedNotification(e.JobId, e.Percentage, e.Message, e.OccurredAt),
            JobCompleted e => new JobCompletedNotification(e.JobId, e.Result, e.OccurredAt),
            JobFailed e => new JobFailedNotification(e.JobId, e.Error, e.RetryCount, e.OccurredAt),
            JobMovedToDeadLetter e => new JobMovedToDeadLetterNotification(e.JobId, e.Reason, e.OccurredAt),
            _ => null
        };
    }
}

// MediatR notification wrappers for domain events
public record JobCreatedNotification(Guid JobId, Guid DataSetId, string OperationType, DateTime OccurredAt) : INotification;
public record JobStartedNotification(Guid JobId, Guid DataSetId, string OperationType, DateTime OccurredAt) : INotification;
public record JobProgressUpdatedNotification(Guid JobId, int Percentage, string Message, DateTime OccurredAt) : INotification;
public record JobCompletedNotification(Guid JobId, string? Result, DateTime OccurredAt) : INotification;
public record JobFailedNotification(Guid JobId, string Error, int RetryCount, DateTime OccurredAt) : INotification;
public record JobMovedToDeadLetterNotification(Guid JobId, string Reason, DateTime OccurredAt) : INotification;