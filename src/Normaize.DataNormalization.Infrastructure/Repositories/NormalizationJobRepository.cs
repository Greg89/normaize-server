using Microsoft.EntityFrameworkCore;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of NormalizationJob repository
/// </summary>
public class NormalizationJobRepository : INormalizationJobRepository
{
    private readonly DataNormalizationDbContext _context;

    public NormalizationJobRepository(DataNormalizationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<NormalizationJob?> GetByIdAsync(Guid jobId)
    {
        return await _context.NormalizationJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);
    }

    public async Task<NormalizationJob?> GetNextQueuedJobAsync()
    {
        return await _context.NormalizationJobs
            .Where(j => j.Status == JobStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(NormalizationJob job)
    {
        _context.NormalizationJobs.Add(job);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(NormalizationJob job)
    {
        _context.NormalizationJobs.Update(job);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid jobId)
    {
        var job = await GetByIdAsync(jobId);
        if (job != null)
        {
            _context.NormalizationJobs.Remove(job);
            await _context.SaveChangesAsync();
        }
    }
}
