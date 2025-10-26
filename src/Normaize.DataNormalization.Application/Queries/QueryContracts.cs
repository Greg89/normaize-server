using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.Application.Queries;

// Job Queries
public record GetJobStatusQuery(Guid JobId);

public record GetJobsQuery(int PageNumber = 1, int PageSize = 20, string? Status = null);

// Analysis Queries
public record GetAnalysisQuery(int AnalysisId);

public record GetAnalysesByDataSetQuery(Guid DataSetId);

public record GetAnalysesByStatusQuery(AnalysisStatus Status);

public record GetAnalysesByTypeQuery(AnalysisType Type);

public record GetAnalysisResultQuery(int AnalysisId);

public record GetAllAnalysesQuery(
    int PageNumber = 1, 
    int PageSize = 20, 
    Guid? DataSetId = null,
    AnalysisStatus? Status = null,
    AnalysisType? Type = null,
    bool IncludeDeleted = false
);

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}
