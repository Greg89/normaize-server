using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Commands.Statistics;

/// <summary>
/// Handler for validating statistical configuration
/// </summary>
public class ValidateStatisticalConfigurationCommandHandler : IRequestHandler<ValidateStatisticalConfigurationCommand, ValidationResultDto>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly ILogger<ValidateStatisticalConfigurationCommandHandler> _logger;

    public ValidateStatisticalConfigurationCommandHandler(
        IDataSetRepository dataSetRepository,
        ILogger<ValidateStatisticalConfigurationCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationResultDto> Handle(ValidateStatisticalConfigurationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating statistical configuration for dataset {DataSetId}", request.DataSetId);

        try
        {
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId);
            if (dataSet == null)
            {
                throw new ArgumentException($"Dataset with ID {request.DataSetId} not found");
            }

            var validationResult = new ValidationResultDto
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                ValidatedColumns = new Dictionary<string, string>()
            };

            // Get actual columns from dataset - placeholder implementation
            // TODO: Implement proper column discovery from dataset schema/structure
            var actualColumns = new HashSet<string>();  // Placeholder for now
            var allSpecifiedColumns = new HashSet<string>();

            // Validate numeric columns
            foreach (var column in request.NumericColumns)
            {
                if (!actualColumns.Contains(column))
                {
                    validationResult.Errors.Add($"Numeric column '{column}' does not exist in dataset");
                    validationResult.IsValid = false;
                }
                else
                {
                    allSpecifiedColumns.Add(column);
                    validationResult.ValidatedColumns[column] = "Numeric";
                }
            }

            // Validate category columns
            foreach (var column in request.CategoryColumns)
            {
                if (!actualColumns.Contains(column))
                {
                    validationResult.Errors.Add($"Category column '{column}' does not exist in dataset");
                    validationResult.IsValid = false;
                }
                else if (allSpecifiedColumns.Contains(column))
                {
                    validationResult.Errors.Add($"Column '{column}' is specified in multiple categories");
                    validationResult.IsValid = false;
                }
                else
                {
                    allSpecifiedColumns.Add(column);
                    validationResult.ValidatedColumns[column] = "Category";
                }
            }

            // Validate ignore columns
            foreach (var column in request.IgnoreColumns)
            {
                if (!actualColumns.Contains(column))
                {
                    validationResult.Warnings.Add($"Ignore column '{column}' does not exist in dataset");
                }
                else if (allSpecifiedColumns.Contains(column))
                {
                    validationResult.Warnings.Add($"Column '{column}' is specified to ignore but also categorized");
                }
                else
                {
                    allSpecifiedColumns.Add(column);
                    validationResult.ValidatedColumns[column] = "Ignored";
                }
            }

            // Check for unspecified columns
            var unspecifiedColumns = actualColumns.Except(allSpecifiedColumns).ToList();
            if (unspecifiedColumns.Any())
            {
                validationResult.Warnings.Add($"Columns not specified in configuration will be auto-detected: {string.Join(", ", unspecifiedColumns)}");
                foreach (var column in unspecifiedColumns)
                {
                    validationResult.ValidatedColumns[column] = "Auto-detect";
                }
            }

            _logger.LogInformation("Statistical configuration validation completed for dataset {DataSetId}. IsValid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                request.DataSetId, validationResult.IsValid, validationResult.Errors.Count, validationResult.Warnings.Count);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating statistical configuration for dataset {DataSetId}", request.DataSetId);
            throw;
        }
    }
}