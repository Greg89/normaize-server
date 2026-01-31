namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Standard API response wrapper.
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
/// Paginated API response wrapper.
/// </summary>
public class PaginatedApiResponse<T> : ApiResponse<T>
{
    public PaginationMetadata? Pagination { get; set; }
}

/// <summary>
/// Pagination metadata.
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
