using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

public interface IDataAnalysisService
{
    Task<AnalysisDto> CreateAnalysisAsync(CreateAnalysisDto createDto);
    Task<AnalysisDto?> GetAnalysisAsync(int id);
    Task<IEnumerable<AnalysisDto>> GetAnalysesByDataSetAsync(int dataSetId);
    Task<IEnumerable<AnalysisDto>> GetAnalysesByStatusAsync(AnalysisStatus status);
    Task<IEnumerable<AnalysisDto>> GetAnalysesByTypeAsync(AnalysisType type);
    Task<AnalysisResultDto> GetAnalysisResultAsync(int analysisId);
    Task<bool> DeleteAnalysisAsync(int id);
    Task<AnalysisDto> RunAnalysisAsync(int analysisId);
} 