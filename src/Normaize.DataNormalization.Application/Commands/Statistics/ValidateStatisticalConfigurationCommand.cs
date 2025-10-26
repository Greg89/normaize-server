using MediatR;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.Application.Commands.Statistics;

/// <summary>
/// Command to validate statistical configuration
/// </summary>
public record ValidateStatisticalConfigurationCommand(
    Guid DataSetId,
    List<string> NumericColumns,
    List<string> CategoryColumns,
    List<string> IgnoreColumns) : IRequest<ValidationResultDto>;