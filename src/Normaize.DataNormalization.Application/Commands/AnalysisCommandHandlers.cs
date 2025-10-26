using System;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Commands;

/// <summary>
/// Command handler for creating new analyses
/// </summary>
public class CreateAnalysisCommandHandler : ICommandHandler<CreateAnalysisCommand, AnalysisDto>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public CreateAnalysisCommandHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AnalysisDto> HandleAsync(CreateAnalysisCommand command)
    {
        // Validate command
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Analysis name is required", nameof(command));

        if (command.DataSetId == Guid.Empty)
            throw new ArgumentException("DataSet ID is required", nameof(command));

        // Create configuration value object if provided
        AnalysisConfiguration? configuration = null;
        if (!string.IsNullOrWhiteSpace(command.Configuration))
        {
            configuration = new AnalysisConfiguration(command.Configuration);
        }

        // Create analysis aggregate
        var analysis = Analysis.Create(
            command.Name,
            command.Description,
            command.Type,
            command.DataSetId,
            command.ComparisonDataSetId,
            configuration);

        // Persist the analysis
        var savedAnalysis = await _analysisRepository.AddAsync(analysis);

        // Map to DTO and return
        return _mapper.ToDto(savedAnalysis);
    }
}

/// <summary>
/// Command handler for running analyses
/// </summary>
public class RunAnalysisCommandHandler : ICommandHandler<RunAnalysisCommand, AnalysisDto>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisExecutionService _executionService;
    private readonly IAnalysisMapper _mapper;

    public RunAnalysisCommandHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisExecutionService executionService,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AnalysisDto> HandleAsync(RunAnalysisCommand command)
    {
        // Get the analysis
        var analysis = await _analysisRepository.GetByIdAsync(new AnalysisId(command.AnalysisId));
        if (analysis == null)
            throw new InvalidOperationException($"Analysis with ID {command.AnalysisId} not found");

        // Start the analysis
        analysis.Start();

        // Update repository with started state
        await _analysisRepository.UpdateAsync(analysis);

        try
        {
            // Execute the analysis
            var result = await _executionService.ExecuteAsync(analysis);

            // Complete the analysis
            analysis.Complete(result);
        }
        catch (Exception ex)
        {
            // Mark as failed
            analysis.Fail(ex.Message);
        }

        // Update repository with final state
        var updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);

        return _mapper.ToDto(updatedAnalysis);
    }
}

/// <summary>
/// Command handler for deleting analyses
/// </summary>
public class DeleteAnalysisCommandHandler : ICommandHandler<DeleteAnalysisCommand, bool>
{
    private readonly IAnalysisRepository _analysisRepository;

    public DeleteAnalysisCommandHandler(IAnalysisRepository analysisRepository)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
    }

    public async Task<bool> HandleAsync(DeleteAnalysisCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DeletedBy))
            throw new ArgumentException("DeletedBy is required", nameof(command));

        return await _analysisRepository.DeleteAsync(new AnalysisId(command.AnalysisId), command.DeletedBy);
    }
}

/// <summary>
/// Command handler for updating analyses
/// </summary>
public class UpdateAnalysisCommandHandler : ICommandHandler<UpdateAnalysisCommand, AnalysisDto>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public UpdateAnalysisCommandHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AnalysisDto> HandleAsync(UpdateAnalysisCommand command)
    {
        // Get the analysis
        var analysis = await _analysisRepository.GetByIdAsync(new AnalysisId(command.AnalysisId));
        if (analysis == null)
            throw new InvalidOperationException($"Analysis with ID {command.AnalysisId} not found");

        // Update details
        analysis.UpdateDetails(command.Name, command.Description);

        // Update configuration if provided
        if (!string.IsNullOrWhiteSpace(command.Configuration))
        {
            var configuration = new AnalysisConfiguration(command.Configuration);
            analysis.UpdateConfiguration(configuration);
        }

        // Save changes
        var updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);

        return _mapper.ToDto(updatedAnalysis);
    }
}

/// <summary>
/// Command handler for resetting analyses
/// </summary>
public class ResetAnalysisCommandHandler : ICommandHandler<ResetAnalysisCommand, AnalysisDto>
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisMapper _mapper;

    public ResetAnalysisCommandHandler(
        IAnalysisRepository analysisRepository,
        IAnalysisMapper mapper)
    {
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AnalysisDto> HandleAsync(ResetAnalysisCommand command)
    {
        // Get the analysis
        var analysis = await _analysisRepository.GetByIdAsync(new AnalysisId(command.AnalysisId));
        if (analysis == null)
            throw new InvalidOperationException($"Analysis with ID {command.AnalysisId} not found");

        // Reset the analysis
        analysis.Reset();

        // Save changes
        var updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);

        return _mapper.ToDto(updatedAnalysis);
    }
}