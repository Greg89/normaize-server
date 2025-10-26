using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Domain.Repositories;

/// <summary>
/// Repository interface for Statistics aggregate
/// </summary>
public interface IStatisticsRepository
{
    /// <summary>
    /// Adds a new statistics record
    /// </summary>
    Task<Statistics> AddAsync(Statistics statistics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics by ID
    /// </summary>
    Task<Statistics?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics by dataset ID
    /// </summary>
    Task<Statistics?> GetByDataSetIdAsync(Guid dataSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all statistics for a user
    /// </summary>
    Task<IEnumerable<Statistics>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing statistics record
    /// </summary>
    Task<Statistics> UpdateAsync(Statistics statistics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes statistics by ID
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes statistics by dataset ID
    /// </summary>
    Task DeleteByDataSetIdAsync(Guid dataSetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if statistics exist for a dataset
    /// </summary>
    Task<bool> ExistsForDataSetAsync(Guid dataSetId, CancellationToken cancellationToken = default);
}