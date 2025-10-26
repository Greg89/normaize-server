using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Events;

/// <summary>
/// Domain event raised when statistics are deleted
/// </summary>
public record StatisticsDeleted(
    StatisticsId StatisticsId,
    Guid DataSetId,
    string DeletedBy) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType { get; } = nameof(StatisticsDeleted);
}