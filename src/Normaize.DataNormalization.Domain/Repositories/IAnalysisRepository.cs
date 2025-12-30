using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Repositories;

/// <summary>
/// Repository interface for Analysis aggregate
/// </summary>
public interface IAnalysisRepository
{
    /// <summary>
    /// Gets an analysis by its identifier
    /// </summary>
    Task<Analysis?> GetByIdAsync(AnalysisId id);

    /// <summary>
    /// Gets all analyses for a specific dataset
    /// </summary>
    Task<IEnumerable<Analysis>> GetByDataSetIdAsync(Guid dataSetId);

    /// <summary>
    /// Gets all analyses with a specific status
    /// </summary>
    Task<IEnumerable<Analysis>> GetByStatusAsync(AnalysisStatus status);

    /// <summary>
    /// Gets all analyses of a specific type
    /// </summary>
    Task<IEnumerable<Analysis>> GetByTypeAsync(AnalysisType type);

    /// <summary>
    /// Gets all analyses (excluding soft-deleted)
    /// </summary>
    Task<IEnumerable<Analysis>> GetAllAsync();

    /// <summary>
    /// Adds a new analysis
    /// </summary>
    Task<Analysis> AddAsync(Analysis analysis);

    /// <summary>
    /// Updates an existing analysis
    /// </summary>
    Task<Analysis> UpdateAsync(Analysis analysis);

    /// <summary>
    /// Soft deletes an analysis
    /// </summary>
    Task<bool> DeleteAsync(AnalysisId id, string deletedBy);

    /// <summary>
    /// Checks if an analysis exists
    /// </summary>
    Task<bool> ExistsAsync(AnalysisId id);

    /// <summary>
    /// Gets analyses by multiple criteria
    /// </summary>
    Task<IEnumerable<Analysis>> GetByCriteriaAsync(
        Guid? dataSetId = null,
        AnalysisStatus? status = null,
        AnalysisType? type = null,
        bool includeDeleted = false);
}