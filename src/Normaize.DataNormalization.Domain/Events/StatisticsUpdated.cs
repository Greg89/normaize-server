using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Events;

/// <summary>
/// Domain event raised when statistics are updated
/// </summary>
public record StatisticsUpdated(
    StatisticsId StatisticsId,
    Guid DataSetId,
    DateTime UpdatedAt) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType { get; } = nameof(StatisticsUpdated);
}