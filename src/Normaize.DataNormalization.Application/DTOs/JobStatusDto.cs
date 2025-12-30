using System;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Application.DTOs;

public record JobStatusDto(
    Guid Id,
    Guid DataSetId,
    string OperationType,
    string OperationParameters,
    string Status,
    int RetryCount,
    int MaxRetries,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    string? Result,
    int ProgressPercentage,
    string? ProgressMessage)
{
    public static JobStatusDto FromDomain(NormalizationJob job)
    {
        return new JobStatusDto(
            job.Id,
            job.DataSetId,
            job.OperationType,
            job.OperationParameters,
            job.Status.ToString(),
            job.RetryCount,
            job.MaxRetries,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.ErrorMessage,
            job.Result,
            job.ProgressPercentage,
            job.ProgressMessage);
    }
}
