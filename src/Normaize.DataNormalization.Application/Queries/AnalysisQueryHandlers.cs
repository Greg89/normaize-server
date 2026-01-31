using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Queries;

/// <summary>
/// Query handler for getting a single analysis
/// </summary>
public class GetAnalysisQueryHandler : IQueryHandler<GetAnalysisQuery, AnalysisDto?>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public GetAnalysisQueryHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AnalysisDto?> HandleAsync(GetAnalysisQuery query)
    {
        var analysis = await _analysisRepository.GetByIdAsync(new AnalysisId(query.AnalysisId));
        return analysis != null ? _mapper.ToDto(analysis) : null;
    }
}

/// <summary>
/// Query handler for getting analyses by dataset
/// </summary>
public class GetAnalysesByDataSetQueryHandler : IQueryHandler<GetAnalysesByDataSetQuery, IEnumerable<AnalysisDto>>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public GetAnalysesByDataSetQueryHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<AnalysisDto>> HandleAsync(GetAnalysesByDataSetQuery query)
    {
        var analyses = await _analysisRepository.GetByDataSetIdAsync(query.DataSetId);
        return analyses.Select(_mapper.ToDto);
    }
}

/// <summary>
/// Query handler for getting analyses by status
/// </summary>
public class GetAnalysesByStatusQueryHandler : IQueryHandler<GetAnalysesByStatusQuery, IEnumerable<AnalysisDto>>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public GetAnalysesByStatusQueryHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<AnalysisDto>> HandleAsync(GetAnalysesByStatusQuery query)
    {
        var analyses = await _analysisRepository.GetByStatusAsync(query.Status);
        return analyses.Select(_mapper.ToDto);
    }
}

/// <summary>
/// Query handler for getting analyses by type
/// </summary>
public class GetAnalysesByTypeQueryHandler : IQueryHandler<GetAnalysesByTypeQuery, IEnumerable<AnalysisDto>>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public GetAnalysesByTypeQueryHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<AnalysisDto>> HandleAsync(GetAnalysesByTypeQuery query)
    {
        var analyses = await _analysisRepository.GetByTypeAsync(query.Type);
        return analyses.Select(_mapper.ToDto);
    }
}

/// <summary>
/// Query handler for getting analysis results
/// </summary>
public class GetAnalysisResultQueryHandler : IQueryHandler<GetAnalysisResultQuery, AnalysisResultDto?>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public GetAnalysisResultQueryHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AnalysisResultDto?> HandleAsync(GetAnalysisResultQuery query)
    {
        var analysis = await _analysisRepository.GetByIdAsync(new AnalysisId(query.AnalysisId));
        if (analysis == null)
            return null;

        return _mapper.ToResultDto(analysis);
    }
}

/// <summary>
/// Query handler for getting all analyses with filtering and pagination
/// </summary>
public class GetAllAnalysesQueryHandler : IQueryHandler<GetAllAnalysesQuery, PaginatedResult<AnalysisDto>>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public GetAllAnalysesQueryHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<PaginatedResult<AnalysisDto>> HandleAsync(GetAllAnalysesQuery query)
    {
        var analyses = await _analysisRepository.GetByCriteriaAsync(
            query.DataSetId,
            query.Status,
            query.Type,
            query.IncludeDeleted);

        var analysisData = analyses.Select(_mapper.ToDto).ToList();

        var totalItems = analysisData.Count;

        // Apply pagination
        var skip = (query.PageNumber - 1) * query.PageSize;

        var items = analysisData
            .Skip(skip)
            .Take(query.PageSize)
            .ToList();

        return new PaginatedResult<AnalysisDto>(items, totalItems);
    }
}