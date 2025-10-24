using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Normaize.DataNormalization.Application.Queries;

public record GetJobStatusQuery(Guid JobId);

public record GetJobsQuery(int PageNumber = 1, int PageSize = 20, string? Status = null);

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}
