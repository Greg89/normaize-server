using System;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing an Analysis identifier
/// </summary>
public record AnalysisId
{
    public int Value { get; init; }

    /// <summary>
    /// Represents an ID for an unpersisted Analysis entity
    /// </summary>
    public static readonly AnalysisId Unpersisted = new(0);

    public AnalysisId(int value)
    {
        if (value < 0)
            throw new ArgumentException("Analysis ID cannot be negative", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Indicates whether this Analysis has been persisted to the database
    /// </summary>
    public bool IsPersisted => Value > 0;

    public static implicit operator int(AnalysisId analysisId) => analysisId.Value;
    public static implicit operator AnalysisId(int value) => new(value);

    public override string ToString() => Value.ToString();
}