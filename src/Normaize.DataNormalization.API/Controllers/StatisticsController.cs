using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary;
using Normaize.DataNormalization.Application.Statistics.Commands.GenerateStatisticalSummary;
using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Statistics.Queries.GetStatistics;
using System.ComponentModel.DataAnnotations;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for statistical calculations and data analysis
/// </summary>
[ApiController]
[Route("api/v1/statistics")]
[Produces("application/json")]
public class StatisticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IMediator mediator, ILogger<StatisticsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate comprehensive data summary for a dataset
    /// </summary>
    /// <param name="dataSetId">The unique identifier of the dataset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data summary with column statistics and quality metrics</returns>
    [HttpPost("data-summary/{dataSetId:guid}")]
    [ProducesResponseType(typeof(DataSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DataSummaryDto>> GenerateDataSummary(
        [FromRoute] Guid dataSetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating data summary for dataset {DataSetId}", dataSetId);

            var command = new GenerateDataSummaryCommand(dataSetId, "system");
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Successfully generated data summary for dataset {DataSetId}", dataSetId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid dataset ID provided: {DataSetId}", dataSetId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Dataset ID",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Dataset not found: {DataSetId}", dataSetId);
            return NotFound(new ProblemDetails
            {
                Title = "Dataset Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data summary for dataset {DataSetId}", dataSetId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while generating the data summary",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Generate statistical summary for numeric columns in a dataset
    /// </summary>
    /// <param name="dataSetId">The unique identifier of the dataset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistical summary with descriptive statistics for numeric columns</returns>
    [HttpPost("statistical-summary/{dataSetId:guid}")]
    [ProducesResponseType(typeof(StatisticalSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StatisticalSummaryDto>> GenerateStatisticalSummary(
        [FromRoute] Guid dataSetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating statistical summary for dataset {DataSetId}", dataSetId);

            var command = new GenerateStatisticalSummaryCommand(dataSetId, "system");
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Successfully generated statistical summary for dataset {DataSetId}", dataSetId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid dataset ID provided: {DataSetId}", dataSetId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Dataset ID",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Dataset not found: {DataSetId}", dataSetId);
            return NotFound(new ProblemDetails
            {
                Title = "Dataset Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating statistical summary for dataset {DataSetId}", dataSetId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while generating the statistical summary",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get existing statistics for a dataset
    /// </summary>
    /// <param name="dataSetId">The unique identifier of the dataset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existing statistics if available</returns>
    [HttpGet("{dataSetId:guid}")]
    [ProducesResponseType(typeof(StatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StatisticsDto>> GetStatistics(
        [FromRoute] Guid dataSetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving statistics for dataset {DataSetId}", dataSetId);

            var query = new GetStatisticsByDataSetIdQuery(dataSetId, "system");
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Statistics Not Found",
                    Detail = $"No statistics found for dataset {dataSetId}",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Successfully retrieved statistics for dataset {DataSetId}", dataSetId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for dataset {DataSetId}", dataSetId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the statistics",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Delete statistics for a dataset
    /// </summary>
    /// <param name="dataSetId">The unique identifier of the dataset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("{dataSetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteStatistics(
        [FromRoute] Guid dataSetId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement DeleteStatisticsCommand
        return NoContent();
        /*
        try
        {
            _logger.LogInformation("Deleting statistics for dataset {DataSetId}", dataSetId);

            var command = new DeleteStatisticsCommand(dataSetId);
            await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Successfully deleted statistics for dataset {DataSetId}", dataSetId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Statistics not found for dataset {DataSetId}", dataSetId);
            return NotFound(new ProblemDetails
            {
                Title = "Statistics Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting statistics for dataset {DataSetId}", dataSetId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while deleting the statistics",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
        */
    }

    /// <summary>
    /// Get correlation matrix for numeric columns in a dataset
    /// </summary>
    /// <param name="dataSetId">The unique identifier of the dataset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Correlation matrix for numeric columns</returns>
    [HttpPost("correlation-matrix/{dataSetId:guid}")]
    [ProducesResponseType(typeof(CorrelationMatrixDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CorrelationMatrixDto>> GetCorrelationMatrix(
        [FromRoute] Guid dataSetId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetCorrelationMatrixQuery
        return NotFound(new ProblemDetails
        {
            Title = "Not Implemented",
            Detail = "Correlation matrix functionality not yet implemented",
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path
        });
    }

    /// <summary>
    /// Validate the statistical configuration and data types
    /// </summary>
    /// <param name="request">Configuration validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate-configuration")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ValidationResultDto>> ValidateConfiguration(
        [FromBody, Required] ConfigurationValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement ValidateStatisticalConfigurationCommand
        return BadRequest(new ProblemDetails
        {
            Title = "Not Implemented",
            Detail = "Configuration validation functionality not yet implemented",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path
        });
    }
}

/// <summary>
/// Request model for configuration validation
/// </summary>
public class ConfigurationValidationRequest
{
    /// <summary>
    /// Dataset identifier
    /// </summary>
    [Required]
    public Guid DataSetId { get; set; }

    /// <summary>
    /// List of columns to treat as numeric
    /// </summary>
    public List<string> NumericColumns { get; set; } = new();

    /// <summary>
    /// List of columns to treat as categorical
    /// </summary>
    public List<string> CategoryColumns { get; set; } = new();

    /// <summary>
    /// List of columns to ignore in calculations
    /// </summary>
    public List<string> IgnoreColumns { get; set; } = new();
}