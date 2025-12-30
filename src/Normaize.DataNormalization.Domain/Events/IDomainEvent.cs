namespace Normaize.DataNormalization.Domain.Events;

/// <summary>
/// Base interface for all domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// When the domain event occurred
    /// </summary>
    DateTime OccurredAt { get; }
}