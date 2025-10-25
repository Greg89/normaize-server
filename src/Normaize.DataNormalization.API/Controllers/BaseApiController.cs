using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Base controller providing common API functionality following DDD principles
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Gets the current user ID from JWT claims (or returns mock user for testing)
    /// </summary>
    protected string GetCurrentUserId()
    {
        // For testing without authentication, return a mock user ID
        return "test-user-id";
        
        // TODO: Re-enable when authentication is working
        /*
        // Debug: Log all available claims
        var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
        Console.WriteLine($"Available claims: {string.Join(", ", allClaims)}");
        
        // Try multiple claim types for user ID
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value
                  ?? User.FindFirst("nameid")?.Value
                  ?? User.FindFirst("user_id")?.Value;
                  
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("User ID not found in any expected claim type");
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        
        Console.WriteLine($"Found User ID: {userId}");
        return userId;
        */
    }

    /// <summary>
    /// Gets the correlation ID for request tracking
    /// </summary>
    protected string GetCorrelationId()
    {
        return HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Creates a successful API response with data
    /// </summary>
    protected IActionResult Success<T>(T data, string? message = null)
    {
        var response = new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully",
            Timestamp = DateTime.UtcNow,
            CorrelationId = GetCorrelationId(),
            DurationMs = _stopwatch.ElapsedMilliseconds
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a successful API response without data
    /// </summary>
    protected IActionResult Success(string? message = null)
    {
        return Success<object?>(null, message);
    }

    /// <summary>
    /// Creates an error API response
    /// </summary>
    protected IActionResult Error(string message, string? errorCode = null, int statusCode = 400)
    {
        var response = new ApiResponse<object?>
        {
            Success = false,
            Data = null,
            Message = message,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow,
            CorrelationId = GetCorrelationId(),
            DurationMs = _stopwatch.ElapsedMilliseconds
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Creates a paginated API response
    /// </summary>
    protected IActionResult SuccessPaginated<T>(T data, int page, int pageSize, int totalItems, string? message = null)
    {
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var response = new PaginatedApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Data retrieved successfully",
            Timestamp = DateTime.UtcNow,
            CorrelationId = GetCorrelationId(),
            DurationMs = _stopwatch.ElapsedMilliseconds,
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Handles exceptions and returns appropriate API response
    /// </summary>
    protected IActionResult HandleException(Exception ex, string operation)
    {
        // Log the exception (would be injected logger in real scenario)
        Console.WriteLine($"Exception in {operation}: {ex}");

        return ex switch
        {
            UnauthorizedAccessException => Error("You are not authorized to perform this action", "UNAUTHORIZED", 401),
            ArgumentException => Error(ex.Message, "INVALID_ARGUMENT", 400),
            InvalidOperationException => Error(ex.Message, "INVALID_OPERATION", 400),
            KeyNotFoundException => Error("The requested resource was not found", "NOT_FOUND", 404),
            NotSupportedException => Error("This operation is not supported", "NOT_SUPPORTED", 405),
            TimeoutException => Error("The operation timed out. Please try again", "TIMEOUT", 408),
            _ => Error("An unexpected error occurred while processing your request", "INTERNAL_ERROR", 500)
        };
    }
}

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

/// <summary>
/// Paginated API response wrapper
/// </summary>
public class PaginatedApiResponse<T> : ApiResponse<T>
{
    public PaginationMetadata? Pagination { get; set; }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}