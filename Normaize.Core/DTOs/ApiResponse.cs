using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Standard API response wrapper for consistent response structure across all endpoints
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The actual data payload
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Error code for client-side error handling
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Metadata about the response
    /// </summary>
    [JsonPropertyName("metadata")]
    public ResponseMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Metadata = ResponseMetadata.Create()
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Metadata = ResponseMetadata.Create()
        };
    }
}

/// <summary>
/// Metadata about the API response
/// </summary>
public class ResponseMetadata
{
    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Request processing duration in milliseconds
    /// </summary>
    [JsonPropertyName("duration")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// Pagination information if applicable
    /// </summary>
    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }

    /// <summary>
    /// Environment information
    /// </summary>
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new metadata instance with current timestamp
    /// </summary>
    public static ResponseMetadata Create()
    {
        return new ResponseMetadata
        {
            Timestamp = DateTime.UtcNow,
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };
    }
}

/// <summary>
/// Pagination information for paginated responses
/// </summary>
public class PaginationInfo
{
    /// <summary>
    /// Current page number
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }
}