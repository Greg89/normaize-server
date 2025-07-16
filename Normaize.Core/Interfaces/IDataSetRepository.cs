using Normaize.Core.Models;
using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

public interface IDataSetRepository
{
    Task<DataSet?> GetByIdAsync(int id);
    Task<IEnumerable<DataSet>> GetAllAsync();
    Task<IEnumerable<DataSet>> GetByUserIdAsync(string userId);
    Task<IEnumerable<DataSet>> GetByUserIdAsync(string userId, bool includeDeleted);
    Task<DataSet> AddAsync(DataSet dataSet);
    Task<DataSet> UpdateAsync(DataSet dataSet);
    Task<bool> DeleteAsync(int id);
    Task<bool> HardDeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<DataSet>> GetDeletedAsync();
    Task<IEnumerable<DataSet>> SearchAsync(string searchTerm, string userId);
    Task<IEnumerable<DataSet>> GetByFileTypeAsync(FileType fileType, string userId);
    Task<IEnumerable<DataSet>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string userId);
    Task<long> GetTotalSizeAsync(string userId);
    Task<int> GetTotalCountAsync(string userId);
    Task<IEnumerable<DataSet>> GetRecentlyModifiedAsync(string userId, int count = 10);
    Task<int> CleanupOldDataSetsAsync(int daysToKeep);
    Task<int> PermanentlyDeleteOldSoftDeletedAsync(int daysToKeep);
} 