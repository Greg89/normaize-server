using Normaize.Core.Models;

namespace Normaize.Data.Repositories;

public interface IDataSetRowRepository
{
    Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(int dataSetId);
    Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(int dataSetId, int skip, int take);
    Task<DataSetRow?> GetByIdAsync(int id);
    Task<DataSetRow> AddAsync(DataSetRow dataSetRow);
    Task<IEnumerable<DataSetRow>> AddRangeAsync(IEnumerable<DataSetRow> dataSetRows);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByDataSetIdAsync(int dataSetId);
    Task<int> GetCountByDataSetIdAsync(int dataSetId);
} 