using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Queries;

public class GetJobStatusQueryHandler : IQueryHandler<GetJobStatusQuery, JobStatusDto?>
{
    private readonly INormalizationJobRepository _repository;

    public GetJobStatusQueryHandler(INormalizationJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<JobStatusDto?> HandleAsync(GetJobStatusQuery query)
    {
        var job = await _repository.GetByIdAsync(query.JobId);
        return job != null ? JobStatusDto.FromDomain(job) : null;
    }
}