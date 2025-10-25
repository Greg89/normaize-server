using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Domain.Repositories;

/// <summary>
/// Repository interface for DataSet entities
/// </summary>
public interface IDataSetRepository
{
    Task<DataSet?> GetByIdAsync(Guid id);
    Task<DataSet?> GetByIdWithRowsAsync(Guid id);
    Task<IEnumerable<DataSet>> GetByUserIdAsync(string userId);
    Task<DataSet> SaveAsync(DataSet dataSet);
    Task<DataSet> UpdateAsync(DataSet dataSet);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
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