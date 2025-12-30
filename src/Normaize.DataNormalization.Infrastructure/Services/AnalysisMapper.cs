using System;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Mapper service for converting between Analysis domain entities and DTOs
/// </summary>
public class AnalysisMapper : IAnalysisMapper
{
    public AnalysisDto ToDto(Analysis analysis)
    {
        if (analysis == null)
            throw new ArgumentNullException(nameof(analysis));

        return new AnalysisDto
        {
            Id = analysis.Id.Value,
            Name = analysis.Name,
            Description = analysis.Description,
            Type = analysis.Type,
            Status = analysis.Status,
            DataSetId = analysis.DataSetId,
            ComparisonDataSetId = analysis.ComparisonDataSetId,
            Configuration = analysis.Configuration?.JsonConfiguration,
            Results = analysis.Result?.JsonResult,
            CreatedAt = analysis.CreatedAt,
            StartedAt = analysis.StartedAt,
            CompletedAt = analysis.CompletedAt,
            ErrorMessage = analysis.ErrorMessage,
            ExecutionDurationMs = analysis.ExecutionDuration?.TotalMilliseconds is double ms ? (long)ms : null,
            IsDeleted = analysis.IsDeleted
        };
    }

    public AnalysisResultDto ToResultDto(Analysis analysis)
    {
        if (analysis == null)
            throw new ArgumentNullException(nameof(analysis));

        return new AnalysisResultDto
        {
            AnalysisId = analysis.Id.Value,
            Status = analysis.Status,
            Results = analysis.Result?.AsObject(),
            ErrorMessage = analysis.ErrorMessage,
            ExecutionDurationMs = analysis.ExecutionDuration?.TotalMilliseconds is double ms2 ? (long)ms2 : null
        };
    }

    public Analysis FromCreateDto(CreateAnalysisDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        AnalysisConfiguration? configuration = null;
        if (!string.IsNullOrWhiteSpace(dto.Configuration))
        {
            configuration = new AnalysisConfiguration(dto.Configuration);
        }

        return Analysis.Create(
            dto.Name,
            dto.Description,
            dto.Type,
            dto.DataSetId,
            dto.ComparisonDataSetId,
            configuration);
    }
}