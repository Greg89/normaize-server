using Normaize.Core.Models;

namespace Normaize.Data.Repositories;

public interface IDataSetRepository
{
    Task<DataSet?> GetByIdAsync(int id);
    Task<IEnumerable<DataSet>> GetAllAsync();
    Task<DataSet> AddAsync(DataSet dataSet);
    Task<DataSet> UpdateAsync(DataSet dataSet);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
} 