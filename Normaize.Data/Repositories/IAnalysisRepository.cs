using Normaize.Core.Models;

namespace Normaize.Data.Repositories;

public interface IAnalysisRepository
{
    Task<Analysis?> GetByIdAsync(int id);
    Task<IEnumerable<Analysis>> GetByDataSetIdAsync(int dataSetId);
    Task<IEnumerable<Analysis>> GetAllAsync();
    Task<Analysis> AddAsync(Analysis analysis);
    Task<Analysis> UpdateAsync(Analysis analysis);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
} 