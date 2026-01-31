using System;
using System.Collections.Generic;
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

    Task<(IReadOnlyList<NormalizationJob> Jobs, int TotalItems)> GetJobsForUserAsync(
        string userId,
        Guid? dataSetId = null,
        JobStatus? status = null,
        string? operationType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 20);

    Task SaveAsync(NormalizationJob job);
    Task UpdateAsync(NormalizationJob job);
    Task DeleteAsync(Guid jobId);
}
