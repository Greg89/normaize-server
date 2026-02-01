using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// EF Core implementation of job queue
/// </summary>
public class EfCoreJobQueue : IJobQueue
{
    private readonly INormalizationJobRepository _repository;

    public EfCoreJobQueue(INormalizationJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task EnqueueAsync(NormalizationJob job)
    {
        await _repository.SaveAsync(job);
    }

    public async Task<NormalizationJob?> DequeueAsync()
    {
        return await _repository.ClaimNextQueuedJobAsync();
    }

    public async Task AckAsync(Guid jobId)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            await _repository.UpdateAsync(job);
        }
    }

    public async Task NackAsync(Guid jobId, string reason, TimeSpan? delay = null)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job != null)
        {
            job.Fail(reason);
            await _repository.UpdateAsync(job);
        }
    }
}
