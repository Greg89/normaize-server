using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Application.Common.Interfaces;

/// <summary>
/// Mapper interface for converting between domain models and DTOs
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps Statistics domain model to DataSummaryDto
    /// </summary>
    DataSummaryDto MapToDataSummaryDto(Domain.Aggregates.Statistics statistics, TimeSpan processingTime);

    /// <summary>
    /// Maps Statistics domain model to StatisticalSummaryDto
    /// </summary>
    StatisticalSummaryDto MapToStatisticalSummaryDto(Domain.Aggregates.Statistics statistics, TimeSpan processingTime);

    /// <summary>
    /// Maps column summary from domain to DTO
    /// </summary>
    BasicColumnSummaryDto MapToColumnSummaryDto(Domain.ValueObjects.ColumnSummary columnSummary);

    /// <summary>
    /// Maps column statistics from domain to DTO
    /// </summary>
    ColumnStatisticsDto MapToColumnStatisticsDto(string columnName, Domain.ValueObjects.StatisticalMeasure statisticalMeasure);

    /// <summary>
    /// Maps data quality summary from domain to DTO
    /// </summary>
    DataQualityScoreDto MapToDataQualityScoreDto(Domain.ValueObjects.DataQualitySummary qualitySummary);
}