using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.Application.Commands;

// Job Commands
public record SubmitJobCommand(Guid DataSetId, string OperationType, string OperationParameters);

public record SubmitDuplicateRemovalJobCommand(Guid DataSetId, DuplicateRemovalOptions Options);

public record RetryJobCommand(Guid JobId);

public record CancelJobCommand(Guid JobId);

// Analysis Commands
public record CreateAnalysisCommand(
    string Name,
    string? Description,
    AnalysisType Type,
    Guid DataSetId,
    Guid? ComparisonDataSetId = null,
    string? Configuration = null
);

public record RunAnalysisCommand(Guid AnalysisId);

public record DeleteAnalysisCommand(Guid AnalysisId, string DeletedBy);

public record UpdateAnalysisCommand(
    Guid AnalysisId,
    string Name,
    string? Description = null,
    string? Configuration = null
);

public record ResetAnalysisCommand(Guid AnalysisId);

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}

public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command);
}
