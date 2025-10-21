using System;
using System.Threading.Tasks;

namespace Normaize.DataNormalization.Application.Commands;

public record SubmitJobCommand(Guid DataSetId, string OperationType, string OperationParameters);

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}

public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command);
}
