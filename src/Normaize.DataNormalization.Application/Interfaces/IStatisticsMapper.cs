using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Application.Interfaces;

/// <summary>
/// Mapper interface for Statistics-specific conversions
/// </summary>
public interface IStatisticsMapper
{
    /// <summary>
    /// Maps Statistics aggregate to StatisticsDto
    /// </summary>
    StatisticsDto MapToStatisticsDto(Domain.Aggregates.Statistics statistics);
}