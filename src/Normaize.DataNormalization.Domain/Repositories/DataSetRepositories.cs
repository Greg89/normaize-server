using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Domain.Repositories;

/// <summary>
/// Repository interface for DataSet entities
/// </summary>
public interface IDataSetRepository
{
    Task<DataSet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataSet?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DataSet?> GetByIdWithRowsAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns all datasets for a user, including soft-deleted datasets.
    /// </summary>
    Task<IReadOnlyList<DataSet>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSet>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSet>> GetDeletedByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<DataSet> AddAsync(DataSet dataSet, CancellationToken cancellationToken = default);
    Task<DataSet> SaveAsync(DataSet dataSet, CancellationToken cancellationToken = default);
    Task<DataSet> UpdateAsync(DataSet dataSet, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for DataSetRow entities
/// </summary>
public interface IDataSetRowRepository
{
    Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(Guid dataSetId);
    Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(Guid dataSetId, int skip, int take);
    Task<DataSetRow?> GetByIdAsync(Guid id);
    Task<DataSetRow> SaveAsync(DataSetRow dataSetRow);
    Task<IEnumerable<DataSetRow>> SaveRangeAsync(IEnumerable<DataSetRow> dataSetRows);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteByDataSetIdAsync(Guid dataSetId);
    Task<int> GetCountByDataSetIdAsync(Guid dataSetId);
}