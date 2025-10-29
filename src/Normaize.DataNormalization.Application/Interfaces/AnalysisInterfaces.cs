using System.Threading.Tasks;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Interfaces;

/// <summary>
/// Service interface for executing analysis operations
/// </summary>
public interface IAnalysisExecutionService
{
    /// <summary>
    /// Executes an analysis and returns the result
    /// </summary>
    Task<AnalysisResult> ExecuteAsync(Analysis analysis);
}

/// <summary>
/// Mapper interface for converting between domain entities and DTOs
/// </summary>
public interface IAnalysisMapper
{
    /// <summary>
    /// Maps an Analysis domain entity to AnalysisDto
    /// </summary>
    AnalysisDto ToDto(Analysis analysis);

    /// <summary>
    /// Maps an Analysis domain entity to AnalysisResultDto
    /// </summary>
    AnalysisResultDto ToResultDto(Analysis analysis);

    /// <summary>
    /// Maps a CreateAnalysisDto to Analysis domain entity
    /// </summary>
    Analysis FromCreateDto(CreateAnalysisDto dto);
}