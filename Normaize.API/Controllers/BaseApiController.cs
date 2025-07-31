using Microsoft.AspNetCore.Mvc;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using System.Diagnostics;

namespace Normaize.API.Controllers;

/// <summary>
/// Base controller providing common API response functionality
/// </summary>
[ApiController]
public abstract class BaseApiController(IStructuredLoggingService? loggingService = null) : ControllerBase
{
    protected readonly IStructuredLoggingService? _loggingService = loggingService;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Creates a successful API response with data
    /// </summary>
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        response.Metadata.CorrelationId = HttpContext.TraceIdentifier;
        response.Metadata.DurationMs = _stopwatch.ElapsedMilliseconds;

        return Ok(response);
    }

    /// <summary>
    /// Creates a successful API response for operations that don't return data
    /// </summary>
    protected ActionResult<ApiResponse<object?>> Success(string? message = null)
    {
        var response = ApiResponse<object?>.SuccessResponse(null, message);
        response.Metadata.CorrelationId = HttpContext.TraceIdentifier;
        response.Metadata.DurationMs = _stopwatch.ElapsedMilliseconds;

        return Ok(response);
    }

    /// <summary>
    /// Creates an error API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Error<T>(string message, string? errorCode = null, int statusCode = 400)
    {
        var response = ApiResponse<T>.ErrorResponse(message, errorCode);
        response.Metadata.CorrelationId = HttpContext.TraceIdentifier;
        response.Metadata.DurationMs = _stopwatch.ElapsedMilliseconds;

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Creates a not found API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "Resource not found")
    {
        return Error<T>(message, "NOT_FOUND", 404);
    }

    /// <summary>
    /// Creates an unauthorized API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Unauthorized<T>(string message = "Unauthorized access")
    {
        return Error<T>(message, "UNAUTHORIZED", 401);
    }

    /// <summary>
    /// Creates a bad request API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> BadRequest<T>(string message = "Invalid request")
    {
        return Error<T>(message, "BAD_REQUEST", 400);
    }

    /// <summary>
    /// Creates an internal server error API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> InternalServerError<T>(string message = "An unexpected error occurred")
    {
        if (_loggingService != null)
        {
            _loggingService.LogException(new Exception(message), $"Internal server error in {GetType().Name}");
        }

        return Error<T>(message, "INTERNAL_SERVER_ERROR", 500);
    }

    /// <summary>
    /// Creates a paginated API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> SuccessPaginated<T>(T data, int page, int pageSize, int totalItems, string? message = null)
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        response.Metadata.CorrelationId = HttpContext.TraceIdentifier;
        response.Metadata.DurationMs = _stopwatch.ElapsedMilliseconds;

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        response.Metadata.Pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };

        return Ok(response);
    }

    /// <summary>
    /// Handles exceptions and returns appropriate API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation)
    {
        if (_loggingService != null)
        {
            _loggingService.LogException(ex, operation);
        }

        return ex switch
        {
            UnauthorizedAccessException => Unauthorized<T>("You are not authorized to perform this action"),
            ArgumentException => BadRequest<T>(ex.Message),
            InvalidOperationException => BadRequest<T>(ex.Message),
            KeyNotFoundException => NotFound<T>("The requested resource was not found"),
            NotSupportedException => Error<T>("This operation is not supported", "NOT_SUPPORTED", 405),
            TimeoutException => Error<T>("The operation timed out. Please try again", "TIMEOUT", 408),
            _ => InternalServerError<T>("An unexpected error occurred while processing your request")
        };
    }
}