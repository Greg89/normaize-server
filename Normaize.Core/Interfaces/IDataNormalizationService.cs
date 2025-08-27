using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for performing data normalization operations on datasets
/// </summary>
public interface IDataNormalizationService
{
    /// <summary>
    /// Submits a duplicate row removal job to the queue
    /// </summary>
    /// <param name="dataSetId">ID of the dataset to normalize</param>
    /// <param name="request">Duplicate removal configuration</param>
    /// <param name="userId">User performing the operation</param>
    /// <param name="correlationId">Correlation ID for distributed tracing</param>
    /// <returns>Job submission response</returns>
    Task<NormalizationJobResponse> SubmitDuplicateRowRemovalJobAsync(
        int dataSetId,
        RemoveDuplicateRowsRequest request,
        string userId,
        string? correlationId = null);

    /// <summary>
    /// Gets the status of a normalization job
    /// </summary>
    /// <param name="jobId">ID of the job to check</param>
    /// <param name="userId">User requesting the status</param>
    /// <returns>Current job status</returns>
    Task<NormalizationJobStatusResponse> GetJobStatusAsync(string jobId, string userId);

    /// <summary>
    /// Cancels a normalization job
    /// </summary>
    /// <param name="jobId">ID of the job to cancel</param>
    /// <param name="userId">User cancelling the job</param>
    /// <returns>Whether the cancellation was successful</returns>
    Task<bool> CancelJobAsync(string jobId, string userId);

    /// <summary>
    /// Gets all normalization jobs for a user
    /// </summary>
    /// <param name="userId">User whose jobs to retrieve</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="includeCompleted">Whether to include completed jobs</param>
    /// <returns>Paginated list of jobs</returns>
    Task<IEnumerable<NormalizationJobStatusResponse>> GetUserJobsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool includeCompleted = false);

    /// <summary>
    /// Gets all normalization jobs for a specific dataset
    /// </summary>
    /// <param name="dataSetId">ID of the dataset</param>
    /// <param name="userId">User requesting the jobs</param>
    /// <returns>List of jobs for the dataset</returns>
    Task<IEnumerable<NormalizationJobStatusResponse>> GetDataSetJobsAsync(int dataSetId, string userId);
}
