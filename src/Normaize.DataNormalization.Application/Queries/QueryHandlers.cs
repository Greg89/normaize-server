using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.Aggregates;

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

public class GetUserJobsQueryHandler : IQueryHandler<GetUserJobsQuery, PaginatedResult<JobStatusDto>>
{
    private readonly INormalizationJobRepository _repository;

    public GetUserJobsQueryHandler(INormalizationJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<PaginatedResult<JobStatusDto>> HandleAsync(GetUserJobsQuery query)
    {
        if (query.PageNumber < 1)
            throw new ArgumentException("PageNumber must be >= 1", nameof(query.PageNumber));

        if (query.PageSize < 1 || query.PageSize > 100)
            throw new ArgumentException("PageSize must be between 1 and 100", nameof(query.PageSize));

        if (query.StartDate.HasValue && query.EndDate.HasValue && query.StartDate.Value > query.EndDate.Value)
            throw new ArgumentException("StartDate must be <= EndDate");

        var parsedStatus = ParseStatus(query.Status);

        var (jobs, totalItems) = await _repository.GetJobsForUserAsync(
            userId: query.UserId,
            dataSetId: query.DataSetId,
            status: parsedStatus,
            operationType: query.JobType,
            startDate: query.StartDate,
            endDate: query.EndDate,
            pageNumber: query.PageNumber,
            pageSize: query.PageSize);

        var items = jobs.Select(JobStatusDto.FromDomain).ToList();
        return new PaginatedResult<JobStatusDto>(items, totalItems);
    }

    private static JobStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        // Back-compat: some clients use "Completed" instead of the domain enum name.
        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            return JobStatus.Succeeded;
        }

        return Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid job status '{status}'.");
    }
}