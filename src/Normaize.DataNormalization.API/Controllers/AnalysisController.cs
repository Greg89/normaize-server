using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// API Controller for Analysis operations following DDD architecture
/// Provides all functionality from legacy IDataAnalysisService
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalysisController : BaseApiController
{
    private readonly ICommandHandler<CreateAnalysisCommand, AnalysisDto> _createAnalysisHandler;
    private readonly ICommandHandler<RunAnalysisCommand, AnalysisDto> _runAnalysisHandler;
    private readonly ICommandHandler<DeleteAnalysisCommand, bool> _deleteAnalysisHandler;
    private readonly ICommandHandler<UpdateAnalysisCommand, AnalysisDto> _updateAnalysisHandler;
    private readonly ICommandHandler<ResetAnalysisCommand, AnalysisDto> _resetAnalysisHandler;
    private readonly IQueryHandler<GetAnalysisQuery, AnalysisDto?> _getAnalysisHandler;
    private readonly IQueryHandler<GetAnalysesByDataSetQuery, IEnumerable<AnalysisDto>> _getAnalysesByDataSetHandler;
    private readonly IQueryHandler<GetAnalysesByStatusQuery, IEnumerable<AnalysisDto>> _getAnalysesByStatusHandler;
    private readonly IQueryHandler<GetAnalysesByTypeQuery, IEnumerable<AnalysisDto>> _getAnalysesByTypeHandler;
    private readonly IQueryHandler<GetAnalysisResultQuery, AnalysisResultDto?> _getAnalysisResultHandler;
    private readonly IQueryHandler<GetAllAnalysesQuery, IEnumerable<AnalysisDto>> _getAllAnalysesHandler;

    public AnalysisController(
        ICommandHandler<CreateAnalysisCommand, AnalysisDto> createAnalysisHandler,
        ICommandHandler<RunAnalysisCommand, AnalysisDto> runAnalysisHandler,
        ICommandHandler<DeleteAnalysisCommand, bool> deleteAnalysisHandler,
        ICommandHandler<UpdateAnalysisCommand, AnalysisDto> updateAnalysisHandler,
        ICommandHandler<ResetAnalysisCommand, AnalysisDto> resetAnalysisHandler,
        IQueryHandler<GetAnalysisQuery, AnalysisDto?> getAnalysisHandler,
        IQueryHandler<GetAnalysesByDataSetQuery, IEnumerable<AnalysisDto>> getAnalysesByDataSetHandler,
        IQueryHandler<GetAnalysesByStatusQuery, IEnumerable<AnalysisDto>> getAnalysesByStatusHandler,
        IQueryHandler<GetAnalysesByTypeQuery, IEnumerable<AnalysisDto>> getAnalysesByTypeHandler,
        IQueryHandler<GetAnalysisResultQuery, AnalysisResultDto?> getAnalysisResultHandler,
        IQueryHandler<GetAllAnalysesQuery, IEnumerable<AnalysisDto>> getAllAnalysesHandler)
    {
        _createAnalysisHandler = createAnalysisHandler ?? throw new ArgumentNullException(nameof(createAnalysisHandler));
        _runAnalysisHandler = runAnalysisHandler ?? throw new ArgumentNullException(nameof(runAnalysisHandler));
        _deleteAnalysisHandler = deleteAnalysisHandler ?? throw new ArgumentNullException(nameof(deleteAnalysisHandler));
        _updateAnalysisHandler = updateAnalysisHandler ?? throw new ArgumentNullException(nameof(updateAnalysisHandler));
        _resetAnalysisHandler = resetAnalysisHandler ?? throw new ArgumentNullException(nameof(resetAnalysisHandler));
        _getAnalysisHandler = getAnalysisHandler ?? throw new ArgumentNullException(nameof(getAnalysisHandler));
        _getAnalysesByDataSetHandler = getAnalysesByDataSetHandler ?? throw new ArgumentNullException(nameof(getAnalysesByDataSetHandler));
        _getAnalysesByStatusHandler = getAnalysesByStatusHandler ?? throw new ArgumentNullException(nameof(getAnalysesByStatusHandler));
        _getAnalysesByTypeHandler = getAnalysesByTypeHandler ?? throw new ArgumentNullException(nameof(getAnalysesByTypeHandler));
        _getAnalysisResultHandler = getAnalysisResultHandler ?? throw new ArgumentNullException(nameof(getAnalysisResultHandler));
        _getAllAnalysesHandler = getAllAnalysesHandler ?? throw new ArgumentNullException(nameof(getAllAnalysesHandler));
    }

    /// <summary>
    /// Creates a new analysis
    /// Corresponds to legacy CreateAnalysisAsync method
    /// </summary>
    /// <param name="request">Analysis creation request</param>
    /// <returns>Created analysis details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CreateAnalysis([FromBody] CreateAnalysisRequest request)
    {
        try
        {
            var command = new CreateAnalysisCommand(
                request.Name,
                request.Description,
                request.Type,
                request.DataSetId,
                request.ComparisonDataSetId,
                request.Configuration);

            var result = await _createAnalysisHandler.HandleAsync(command);
            return Success(result, "Analysis created successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(CreateAnalysis));
        }
    }

    /// <summary>
    /// Gets an analysis by ID
    /// Corresponds to legacy GetAnalysisAsync method
    /// </summary>
    /// <param name="id">Analysis ID</param>
    /// <returns>Analysis details if found</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAnalysis(int id)
    {
        try
        {
            var query = new GetAnalysisQuery(id);
            var result = await _getAnalysisHandler.HandleAsync(query);

            if (result == null)
                return Error("Analysis not found", "NOT_FOUND", 404);

            return Success(result, "Analysis retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAnalysis));
        }
    }

    /// <summary>
    /// Gets all analyses for a specific dataset
    /// Corresponds to legacy GetAnalysesByDataSetAsync method
    /// </summary>
    /// <param name="dataSetId">Dataset ID</param>
    /// <returns>List of analyses for the dataset</returns>
    [HttpGet("dataset/{dataSetId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AnalysisDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAnalysesByDataSet(Guid dataSetId)
    {
        try
        {
            var query = new GetAnalysesByDataSetQuery(dataSetId);
            var result = await _getAnalysesByDataSetHandler.HandleAsync(query);
            return Success(result, "Analyses retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAnalysesByDataSet));
        }
    }

    /// <summary>
    /// Gets all analyses with a specific status
    /// Corresponds to legacy GetAnalysesByStatusAsync method
    /// </summary>
    /// <param name="status">Analysis status</param>
    /// <returns>List of analyses with the specified status</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AnalysisDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAnalysesByStatus(AnalysisStatus status)
    {
        try
        {
            var query = new GetAnalysesByStatusQuery(status);
            var result = await _getAnalysesByStatusHandler.HandleAsync(query);
            return Success(result, "Analyses retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAnalysesByStatus));
        }
    }

    /// <summary>
    /// Gets all analyses of a specific type
    /// Corresponds to legacy GetAnalysesByTypeAsync method
    /// </summary>
    /// <param name="type">Analysis type</param>
    /// <returns>List of analyses of the specified type</returns>
    [HttpGet("type/{type}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AnalysisDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAnalysesByType(AnalysisType type)
    {
        try
        {
            var query = new GetAnalysesByTypeQuery(type);
            var result = await _getAnalysesByTypeHandler.HandleAsync(query);
            return Success(result, "Analyses retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAnalysesByType));
        }
    }

    /// <summary>
    /// Gets analysis results
    /// Corresponds to legacy GetAnalysisResultAsync method
    /// </summary>
    /// <param name="id">Analysis ID</param>
    /// <returns>Analysis results if available</returns>
    [HttpGet("{id:int}/result")]
    [ProducesResponseType(typeof(ApiResponse<AnalysisResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAnalysisResult(int id)
    {
        try
        {
            var query = new GetAnalysisResultQuery(id);
            var result = await _getAnalysisResultHandler.HandleAsync(query);

            if (result == null)
                return Error("Analysis not found", "NOT_FOUND", 404);

            return Success(result, "Analysis result retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAnalysisResult));
        }
    }

    /// <summary>
    /// Runs/executes an analysis
    /// Corresponds to legacy RunAnalysisAsync method
    /// </summary>
    /// <param name="id">Analysis ID</param>
    /// <returns>Updated analysis with execution results</returns>
    [HttpPost("{id:int}/run")]
    [ProducesResponseType(typeof(ApiResponse<AnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> RunAnalysis(int id)
    {
        try
        {
            var command = new RunAnalysisCommand(id);
            var result = await _runAnalysisHandler.HandleAsync(command);
            return Success(result, "Analysis executed successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(RunAnalysis));
        }
    }

    /// <summary>
    /// Deletes an analysis (soft delete)
    /// Corresponds to legacy DeleteAnalysisAsync method
    /// </summary>
    /// <param name="id">Analysis ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> DeleteAnalysis(int id)
    {
        try
        {
            var command = new DeleteAnalysisCommand(id, GetCurrentUserId());
            var result = await _deleteAnalysisHandler.HandleAsync(command);

            if (!result)
                return Error("Analysis not found or could not be deleted", "NOT_FOUND", 404);

            return Success(result, "Analysis deleted successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(DeleteAnalysis));
        }
    }

    /// <summary>
    /// Gets all analyses with optional filtering and pagination
    /// Enhanced version providing comprehensive analysis listing
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="dataSetId">Optional dataset filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="type">Optional type filter</param>
    /// <param name="includeDeleted">Include soft-deleted analyses (default: false)</param>
    /// <returns>Paginated list of analyses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedApiResponse<IEnumerable<AnalysisDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAllAnalyses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? dataSetId = null,
        [FromQuery] AnalysisStatus? status = null,
        [FromQuery] AnalysisType? type = null,
        [FromQuery] bool includeDeleted = false)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1)
                return Error("Page number must be greater than 0", "INVALID_PAGE");

            if (pageSize < 1 || pageSize > 100)
                return Error("Page size must be between 1 and 100", "INVALID_PAGE_SIZE");

            var query = new GetAllAnalysesQuery(page, pageSize, dataSetId, status, type, includeDeleted);
            var result = await _getAllAnalysesHandler.HandleAsync(query);
            var analyses = result.ToList();

            // For simplicity, we're not implementing true pagination count here
            // In a real scenario, you'd want a separate count query
            var totalItems = analyses.Count;

            return SuccessPaginated(analyses, page, pageSize, totalItems, "Analyses retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAllAnalyses));
        }
    }

    /// <summary>
    /// Updates an analysis
    /// Additional functionality for analysis management
    /// </summary>
    /// <param name="id">Analysis ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated analysis</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> UpdateAnalysis(int id, [FromBody] UpdateAnalysisRequest request)
    {
        try
        {
            var command = new UpdateAnalysisCommand(id, request.Name, request.Description, request.Configuration);
            var result = await _updateAnalysisHandler.HandleAsync(command);
            return Success(result, "Analysis updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UpdateAnalysis));
        }
    }

    /// <summary>
    /// Resets an analysis to pending state
    /// Additional functionality for analysis management
    /// </summary>
    /// <param name="id">Analysis ID</param>
    /// <returns>Reset analysis</returns>
    [HttpPost("{id:int}/reset")]
    [ProducesResponseType(typeof(ApiResponse<AnalysisDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ResetAnalysis(int id)
    {
        try
        {
            var command = new ResetAnalysisCommand(id);
            var result = await _resetAnalysisHandler.HandleAsync(command);
            return Success(result, "Analysis reset successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(ResetAnalysis));
        }
    }
}

/// <summary>
/// Request model for creating analysis
/// </summary>
public class CreateAnalysisRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public AnalysisType Type { get; set; }

    [Required]
    public Guid DataSetId { get; set; }

    public Guid? ComparisonDataSetId { get; set; }

    [StringLength(5000)]
    public string? Configuration { get; set; }
}

/// <summary>
/// Request model for updating analysis
/// </summary>
public class UpdateAnalysisRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(5000)]
    public string? Configuration { get; set; }
}