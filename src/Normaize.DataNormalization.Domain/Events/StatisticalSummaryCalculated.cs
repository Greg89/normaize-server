using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Events;

/// <summary>
/// Domain event raised when comprehensive statistical summary is calculated
/// </summary>
public record StatisticalSummaryCalculated(
    StatisticsId StatisticsId,
    Guid DataSetId,
    int StatisticalColumnCount,
    int SkewedColumnCount,
    int HighKurtosisColumnCount) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType { get; } = nameof(StatisticalSummaryCalculated);
}