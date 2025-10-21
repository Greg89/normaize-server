using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Domain.Repositories;

/// <summary>
/// Repository interface for NormalizationJob aggregate
/// </summary>
public interface INormalizationJobRepository
{
    Task<NormalizationJob?> GetByIdAsync(Guid jobId);
    Task<NormalizationJob?> GetNextQueuedJobAsync();
    Task SaveAsync(NormalizationJob job);
    Task UpdateAsync(NormalizationJob job);
    Task DeleteAsync(Guid jobId);
}
