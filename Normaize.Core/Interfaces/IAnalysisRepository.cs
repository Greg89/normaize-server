using Normaize.Core.Models;
using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

public interface IAnalysisRepository
{
    Task<Analysis?> GetByIdAsync(int id);
    Task<IEnumerable<Analysis>> GetByDataSetIdAsync(int dataSetId);
    Task<IEnumerable<Analysis>> GetAllAsync();
    Task<Analysis> AddAsync(Analysis analysis);
    Task<Analysis> UpdateAsync(Analysis analysis);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<Analysis>> GetByStatusAsync(AnalysisStatus status);
    Task<IEnumerable<Analysis>> GetByTypeAsync(AnalysisType type);
}