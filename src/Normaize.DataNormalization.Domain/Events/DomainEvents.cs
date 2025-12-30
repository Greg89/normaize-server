using System;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Events;

// Normalization Job Events
public record JobCreated(Guid JobId, Guid DataSetId, string OperationType) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record JobStarted(Guid JobId, Guid DataSetId, string OperationType) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record JobProgressUpdated(Guid JobId, int Percentage, string Message) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record JobCompleted(Guid JobId, string? Result) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record JobFailed(Guid JobId, string Error, int RetryCount) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record JobMovedToDeadLetter(Guid JobId, string Reason) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// Analysis Events
public record AnalysisCreated(AnalysisId AnalysisId, Guid DataSetId, AnalysisType Type, string Name) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record AnalysisStarted(AnalysisId AnalysisId, AnalysisType Type) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record AnalysisCompleted(AnalysisId AnalysisId, AnalysisResult Result) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record AnalysisFailed(AnalysisId AnalysisId, string ErrorMessage) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record AnalysisDeleted(AnalysisId AnalysisId, string DeletedBy) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
