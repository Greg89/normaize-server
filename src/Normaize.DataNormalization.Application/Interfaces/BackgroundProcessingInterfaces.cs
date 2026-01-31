using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Application.Interfaces;

/// <summary>
/// Queue abstraction for job processing
/// </summary>
public interface IJobQueue
{
    Task EnqueueAsync(NormalizationJob job);
    Task<NormalizationJob?> DequeueAsync();
    Task AckAsync(Guid jobId);
    Task NackAsync(Guid jobId, string reason, TimeSpan? delay = null);
}

/// <summary>
/// Progress reporting interface
/// </summary>
public interface IJobProgress
{
    Task StartedAsync(Guid jobId);
    Task ReportAsync(Guid jobId, int percent, string message);
    Task SucceededAsync(Guid jobId, object? result);
    Task FailedAsync(Guid jobId, string error);
}

/// <summary>
/// Job router for dispatching to appropriate handlers
/// </summary>
public interface INormalizationJobRouter
{
    Task HandleAsync(NormalizationJob job, IJobProgress progress);
}

/// <summary>
/// Handler for duplicate row removal operations
/// </summary>
public interface IRemoveDuplicatesHandler
{
    Task HandleAsync(NormalizationJob job, IJobProgress progress);
}

/// <summary>
/// Handler for processing uploaded dataset files asynchronously
/// Extracts schema, preview data, and statistics
/// </summary>
public interface IProcessDataSetFileHandler
{
    Task HandleAsync(NormalizationJob job, IJobProgress progress);
}
