using System;

namespace Normaize.DataNormalization.Domain.Events;

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
