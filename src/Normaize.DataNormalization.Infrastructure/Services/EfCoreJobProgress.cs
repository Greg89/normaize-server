using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// EF Core implementation of job progress reporting
/// </summary>
public class EfCoreJobProgress : IJobProgress
{
    private readonly INormalizationJobRepository _repository;

    public EfCoreJobProgress(INormalizationJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task StartedAsync(Guid jobId)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Start();
            await _repository.UpdateAsync(job);
        }
    }

    public async Task ReportAsync(Guid jobId, int percent, string message)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.UpdateProgress(percent, message);
            await _repository.UpdateAsync(job);
        }
    }

    public async Task SucceededAsync(Guid jobId, object? result)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Complete(result);
            await _repository.UpdateAsync(job);
        }
    }

    public async Task FailedAsync(Guid jobId, string error)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Fail(error);
            await _repository.UpdateAsync(job);
        }
    }
}
