using System;
using System.Collections.Generic;

namespace Normaize.DataNormalization.Domain.Aggregates;

/// <summary>
/// Represents a data normalization job aggregate root
/// </summary>
public class NormalizationJob
{
    public Guid Id { get; private set; }
    public Guid DataSetId { get; private set; }
    public string OperationType { get; private set; }
    public string OperationParameters { get; private set; }
    public JobStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Result { get; private set; }
    public int ProgressPercentage { get; private set; }
    public string? ProgressMessage { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private NormalizationJob() { } // EF Core constructor

    public static NormalizationJob Create(Guid dataSetId, string operationType, string operationParameters, int maxRetries = 5)
    {
        var job = new NormalizationJob
        {
            Id = Guid.NewGuid(),
            DataSetId = dataSetId,
            OperationType = operationType,
            OperationParameters = operationParameters,
            Status = JobStatus.Queued,
            RetryCount = 0,
            MaxRetries = maxRetries,
            CreatedAt = DateTime.UtcNow,
            ProgressPercentage = 0
        };

        job._domainEvents.Add(new JobCreated(job.Id, job.DataSetId, job.OperationType));
        return job;
    }

    public void Start()
    {
        if (Status != JobStatus.Queued)
            throw new InvalidOperationException($"Cannot start job in {Status} status");

        Status = JobStatus.Processing;
        StartedAt = DateTime.UtcNow;
        _domainEvents.Add(new JobStarted(Id, DataSetId, OperationType));
    }

    public void UpdateProgress(int percentage, string message)
    {
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException($"Cannot update progress for job in {Status} status");

        ProgressPercentage = Math.Clamp(percentage, 0, 100);
        ProgressMessage = message;
        _domainEvents.Add(new JobProgressUpdated(Id, ProgressPercentage, ProgressMessage));
    }

    public void Complete(object? result)
    {
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException($"Cannot complete job in {Status} status");

        Status = JobStatus.Succeeded;
        CompletedAt = DateTime.UtcNow;
        ProgressPercentage = 100;
        Result = result?.ToString();
        _domainEvents.Add(new JobCompleted(Id, Result));
    }

    public void Fail(string errorMessage)
    {
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException($"Cannot fail job in {Status} status");

        Status = JobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        _domainEvents.Add(new JobFailed(Id, ErrorMessage, RetryCount));
    }

    public void ScheduleRetry(DateTime retryAt)
    {
        if (Status != JobStatus.Failed)
            throw new InvalidOperationException($"Cannot retry job in {Status} status");

        if (RetryCount >= MaxRetries)
            throw new InvalidOperationException("Maximum retry count exceeded");

        RetryCount++;
        Status = JobStatus.Queued;
        StartedAt = null;
        CompletedAt = null;
        ErrorMessage = null;
        ProgressPercentage = 0;
        ProgressMessage = null;
    }

    public void MoveToDeadLetter(string reason)
    {
        Status = JobStatus.DeadLettered;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = reason;
        _domainEvents.Add(new JobMovedToDeadLetter(Id, reason));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public enum JobStatus
{
    Queued,
    Processing,
    Succeeded,
    Failed,
    DeadLettered
}

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
