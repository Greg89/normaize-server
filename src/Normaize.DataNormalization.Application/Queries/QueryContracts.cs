using System;
using System.Threading.Tasks;

namespace Normaize.DataNormalization.Application.Queries;

public record GetJobStatusQuery(Guid JobId);

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}
