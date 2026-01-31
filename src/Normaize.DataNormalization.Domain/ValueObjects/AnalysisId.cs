using System;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing an Analysis identifier
/// </summary>
public record AnalysisId
{
    public Guid Value { get; init; }

    /// <summary>
    /// Represents an ID for an unpersisted Analysis entity
    /// </summary>
    public static readonly AnalysisId Unpersisted = new(Guid.Empty);

    public AnalysisId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new AnalysisId with a generated Guid
    /// </summary>
    public static AnalysisId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Indicates whether this Analysis has been persisted to the database
    /// </summary>
    public bool IsPersisted => Value != Guid.Empty;

    public static implicit operator Guid(AnalysisId analysisId) => analysisId.Value;
    public static implicit operator AnalysisId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}