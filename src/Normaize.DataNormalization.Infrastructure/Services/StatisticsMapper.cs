using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Implementation of mapper for converting between domain models and DTOs
/// </summary>
public class StatisticsMapper : IMapper, IStatisticsMapper
{
    public DataSummaryDto MapToDataSummaryDto(Statistics statistics, TimeSpan processingTime)
    {
        var qualityScore = MapToDataQualityScoreDto(statistics.GetDataQualitySummary());

        return new DataSummaryDto
        {
            DataSetId = statistics.DataSetId,
            TotalRows = statistics.TotalRows,
            TotalColumns = statistics.TotalColumns,
            MissingValues = statistics.MissingValues,
            DuplicateRows = statistics.DuplicateRows,
            ColumnSummaries = statistics.ColumnSummaries.ToDictionary(
                kvp => kvp.Key,
                kvp => MapToColumnSummaryDto(kvp.Value)),
            GeneratedAt = statistics.CalculatedAt,
            ProcessingTime = processingTime,
            QualityScore = qualityScore
        };
    }

    public StatisticalSummaryDto MapToStatisticalSummaryDto(Statistics statistics, TimeSpan processingTime)
    {
        var numericStats = statistics.GetNumericColumnStatistics();
        var insights = CreateStatisticalInsights(numericStats, statistics.GetDataQualitySummary());

        return new StatisticalSummaryDto
        {
            DataSetId = statistics.DataSetId,
            ColumnStatistics = numericStats.ToDictionary(
                kvp => kvp.Key,
                kvp => MapToColumnStatisticsDto(kvp.Key, kvp.Value)),
            CorrelationMatrix = new Dictionary<string, double>(), // Could be calculated if needed
            OutlierColumns = numericStats
                .Where(kvp => kvp.Value.OutlierCount > 0)
                .Select(kvp => kvp.Key)
                .ToList(),
            OutlierIndices = new List<int>(), // Would need additional calculation
            GeneratedAt = statistics.CalculatedAt,
            ProcessingTime = processingTime,
            Insights = insights
        };
    }

    public Application.Common.DTOs.BasicColumnSummaryDto MapToColumnSummaryDto(ColumnSummary columnSummary)
    {
        return new Application.Common.DTOs.BasicColumnSummaryDto
        {
            ColumnName = columnSummary.ColumnName,
            DataType = columnSummary.DataType.TypeName,
            NonNullCount = columnSummary.NonNullCount,
            NullCount = columnSummary.NullCount,
            UniqueCount = columnSummary.UniqueCount,
            SampleValues = columnSummary.SampleValues.ToList(),
            MinValue = columnSummary.MinValue,
            MaxValue = columnSummary.MaxValue,
            Mean = columnSummary.Statistics?.Mean,
            Median = columnSummary.Statistics?.Median,
            StandardDeviation = columnSummary.Statistics?.StandardDeviation
        };
    }

    public ColumnStatisticsDto MapToColumnStatisticsDto(string columnName, StatisticalMeasure statisticalMeasure)
    {
        return new ColumnStatisticsDto
        {
            ColumnName = columnName,
            Mean = statisticalMeasure.Mean,
            Median = statisticalMeasure.Median,
            StandardDeviation = statisticalMeasure.StandardDeviation,
            Min = statisticalMeasure.Min,
            Max = statisticalMeasure.Max,
            Q1 = statisticalMeasure.Q1,
            Q2 = statisticalMeasure.Q2,
            Q3 = statisticalMeasure.Q3,
            Skewness = statisticalMeasure.Skewness,
            Kurtosis = statisticalMeasure.Kurtosis,
            OutlierCount = statisticalMeasure.OutlierCount,
            Range = statisticalMeasure.Range,
            InterquartileRange = statisticalMeasure.InterquartileRange,
            IsSignificantlySkewed = statisticalMeasure.IsSignificantlySkewed,
            IsHighKurtosis = statisticalMeasure.IsHighKurtosis
        };
    }

    public DataQualityScoreDto MapToDataQualityScoreDto(DataQualitySummary qualitySummary)
    {
        return new DataQualityScoreDto
        {
            OverallScore = qualitySummary.QualityScore,
            ColumnsWithHighNullRate = qualitySummary.ColumnsWithHighNullRate,
            HighCardinalityColumns = qualitySummary.HighCardinalityColumns,
            MissingDataPercentage = qualitySummary.MissingDataPercentage,
            DuplicateRowsPercentage = qualitySummary.DuplicateRowsPercentage,
            HasQualityIssues = qualitySummary.HasQualityIssues,
            HasSeriousIssues = qualitySummary.HasSeriousIssues
        };
    }

    public StatisticsDto MapToStatisticsDto(Statistics statistics)
    {
        return new StatisticsDto
        {
            Id = statistics.Id.Value,
            DataSetId = statistics.DataSetId,
            DataSetName = statistics.DataSetName,
            TotalRows = statistics.TotalRows,
            TotalColumns = statistics.TotalColumns,
            MissingValues = statistics.MissingValues,
            DuplicateRows = statistics.DuplicateRows,
            CalculatedAt = statistics.CalculatedAt,
            ProcessingTime = statistics.ProcessingTime,
            ColumnSummaries = statistics.ColumnSummaries.ToDictionary(
                kvp => kvp.Key,
                kvp => MapToCompleteColumnSummaryDto(kvp.Value)),
            ColumnStatistics = statistics.ColumnStatistics.ToDictionary(
                kvp => kvp.Key,
                kvp => MapToStatisticalMeasureDto(kvp.Value))
        };
    }

    private StatisticalMeasureDto MapToStatisticalMeasureDto(StatisticalMeasure measure)
    {
        return new StatisticalMeasureDto
        {
            Mean = measure.Mean,
            Median = measure.Median,
            StandardDeviation = measure.StandardDeviation,
            Min = measure.Min,
            Max = measure.Max,
            Q1 = measure.Q1,
            Q2 = measure.Q2,
            Q3 = measure.Q3,
            Skewness = measure.Skewness,
            Kurtosis = measure.Kurtosis,
            OutlierCount = measure.OutlierCount
        };
    }

    private Application.DTOs.DetailedColumnSummaryDto MapToCompleteColumnSummaryDto(ColumnSummary columnSummary)
    {
        return new Application.DTOs.DetailedColumnSummaryDto
        {
            ColumnName = columnSummary.ColumnName,
            DataType = new DataTypeClassificationDto
            {
                TypeName = columnSummary.DataType.TypeName,
                IsNumeric = columnSummary.DataType.IsNumeric,
                IsDateTime = columnSummary.DataType.IsDateTime,
                IsBoolean = columnSummary.DataType.IsBoolean
            },
            NonNullCount = columnSummary.NonNullCount,
            NullCount = columnSummary.NullCount,
            UniqueCount = columnSummary.UniqueCount,
            SampleValues = columnSummary.SampleValues.ToList(),
            MinValue = columnSummary.MinValue,
            MaxValue = columnSummary.MaxValue,
            Statistics = columnSummary.Statistics != null
                ? MapToStatisticalMeasureDto(columnSummary.Statistics)
                : null
        };
    }

    private static StatisticalInsightsDto CreateStatisticalInsights(
        IReadOnlyDictionary<string, StatisticalMeasure> numericStats,
        DataQualitySummary qualitySummary)
    {
        var recommendations = new List<string>();
        var warnings = new List<string>();

        var skewedColumns = numericStats.Values.Count(s => s.IsSignificantlySkewed);
        var highKurtosisColumns = numericStats.Values.Count(s => s.IsHighKurtosis);

        // Generate recommendations based on statistical properties
        if (skewedColumns > 0)
        {
            recommendations.Add($"Consider log transformation for {skewedColumns} skewed column(s)");
        }

        if (highKurtosisColumns > 0)
        {
            recommendations.Add($"Review {highKurtosisColumns} column(s) with high kurtosis for outliers");
        }

        // Note: OutlierCount is not available in DataQualitySummary
        // Could add outlier recommendations based on statistical measures instead

        // Generate warnings based on data quality
        if (qualitySummary.HasSeriousIssues)
        {
            warnings.Add("Serious data quality issues detected");
        }

        if (qualitySummary.MissingDataPercentage > 25)
        {
            warnings.Add($"High missing data rate: {qualitySummary.MissingDataPercentage:F1}%");
        }

        if (qualitySummary.DuplicateRowsPercentage > 10)
        {
            warnings.Add($"High duplicate rate: {qualitySummary.DuplicateRowsPercentage:F1}%");
        }

        return new StatisticalInsightsDto
        {
            NumericColumnCount = numericStats.Count,
            SkewedColumnCount = skewedColumns,
            HighKurtosisColumnCount = highKurtosisColumns,
            RecommendedTransformations = recommendations,
            DataQualityWarnings = warnings
        };
    }
}