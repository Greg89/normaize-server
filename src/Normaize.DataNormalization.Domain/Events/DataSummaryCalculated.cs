using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Events;

/// <summary>
/// Domain event raised when basic data summary is calculated
/// </summary>
public record DataSummaryCalculated(
    StatisticsId StatisticsId,
    Guid DataSetId,
    int TotalRows,
    int TotalColumns,
    int MissingValues,
    int DuplicateRows,
    int ColumnCount) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType { get; } = nameof(DataSummaryCalculated);
}