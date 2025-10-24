using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event asynchronously
    /// </summary>
    /// <param name="domainEvent">The domain event to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync(IDomainEvent domainEvent);
}