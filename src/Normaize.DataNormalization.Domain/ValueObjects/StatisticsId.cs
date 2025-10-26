namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing a Statistics identifier
/// </summary>
public record StatisticsId
{
    public int Value { get; init; }

    /// <summary>
    /// Represents an ID for an unpersisted Statistics entity
    /// </summary>
    public static readonly StatisticsId Unpersisted = new(0);

    public StatisticsId(int value)
    {
        if (value < 0)
            throw new ArgumentException("Statistics ID cannot be negative", nameof(value));
        
        Value = value;
    }

    /// <summary>
    /// Indicates whether this Statistics has been persisted to the database
    /// </summary>
    public bool IsPersisted => Value > 0;

    public static implicit operator int(StatisticsId statisticsId) => statisticsId.Value;
    public static implicit operator StatisticsId(int value) => new(value);

    public override string ToString() => Value.ToString();
}