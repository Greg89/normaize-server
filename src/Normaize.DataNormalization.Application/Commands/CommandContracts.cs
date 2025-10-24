using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Commands;

public record SubmitJobCommand(Guid DataSetId, string OperationType, string OperationParameters);

public record SubmitDuplicateRemovalJobCommand(Guid DataSetId, DuplicateRemovalOptions Options);

public record RetryJobCommand(Guid JobId);

public record CancelJobCommand(Guid JobId);

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}

public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command);
}
