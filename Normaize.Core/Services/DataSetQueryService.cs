using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Mapping;
using System.Diagnostics;

namespace Normaize.Core.Services;

/// <summary>
/// Service for dataset querying, searching, and filtering operations.
/// </summary>
public class DataSetQueryService : IDataSetQueryService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public DataSetQueryService(
        IDataSetRepository dataSetRepository,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(infrastructure);
        _dataSetRepository = dataSetRepository;
        _infrastructure = infrastructure;
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId, int page = 1, int pageSize = 20)
    {
                    return await ExecuteQueryOperationAsync(
            AppConstants.DataSetQuery.GET_DATA_SETS_BY_USER,
            userId,
            new Dictionary<string, object> { ["Page"] = page, ["PageSize"] = pageSize },
            () => ValidateQueryInputs(userId, page, pageSize),
            async (context) =>
            {
                // Chaos engineering: Simulate network latency during dataset retrieval
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("NetworkLatency", context.CorrelationId, context.OperationName, async () =>
                {
                    var delayMs = new Random().Next(AppConstants.ChaosEngineering.MIN_NETWORK_LATENCY_MS, AppConstants.ChaosEngineering.MAX_NETWORK_LATENCY_MS);
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating network latency during dataset retrieval", new Dictionary<string, object>
                    {
                        ["DelayMs"] = delayMs,
                        ["ChaosType"] = "NetworkLatency"
                    });
                    await Task.Delay(delayMs);
                }, new Dictionary<string, object> { ["UserId"] = userId, ["Page"] = page, ["PageSize"] = pageSize });

                var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
                var activeDataSets = dataSets.Where(ds => !ds.IsDeleted);
                var paginatedDataSets = ApplyPagination(activeDataSets, page, pageSize, context);
                
                return paginatedDataSets.Select(ds => ds.ToDto());
            });
    }

    public async Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteQueryOperationAsync(
            AppConstants.DataSetQuery.GET_DELETED_DATA_SETS,
            userId,
            new Dictionary<string, object> { ["Page"] = page, ["PageSize"] = pageSize },
            () => ValidateQueryInputs(userId, page, pageSize),
            async (context) =>
            {
                var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
                var deletedDataSets = dataSets.Where(ds => ds.IsDeleted);
                var paginatedDataSets = ApplyPagination(deletedDataSets, page, pageSize, context);
                
                return paginatedDataSets.Select(ds => ds.ToDto());
            });
    }

    public async Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteQueryOperationAsync(
            AppConstants.DataSetQuery.SEARCH_DATA_SETS,
            userId,
            new Dictionary<string, object> { ["SearchTerm"] = searchTerm, ["Page"] = page, ["PageSize"] = pageSize },
            () => ValidateSearchInputs(searchTerm, userId, page, pageSize),
            async (context) =>
            {
                var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
                var activeDataSets = dataSets.Where(ds => !ds.IsDeleted);
                
                var searchResults = activeDataSets.Where(ds =>
                    ds.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    ds.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    ds.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                
                var paginatedDataSets = ApplyPagination(searchResults, page, pageSize, context);
                
                return paginatedDataSets.Select(ds => ds.ToDto());
            });
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(FileType fileType, string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteQueryOperationAsync(
            AppConstants.DataSetQuery.GET_DATA_SETS_BY_FILE_TYPE,
            userId,
            new Dictionary<string, object> { ["FileType"] = fileType.ToString(), ["Page"] = page, ["PageSize"] = pageSize },
            () => ValidateQueryInputs(userId, page, pageSize),
            async (context) =>
            {
                var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
                var activeDataSets = dataSets.Where(ds => !ds.IsDeleted && ds.FileType == fileType);
                var paginatedDataSets = ApplyPagination(activeDataSets, page, pageSize, context);
                
                return paginatedDataSets.Select(ds => ds.ToDto());
            });
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteQueryOperationAsync(
            AppConstants.DataSetQuery.GET_DATA_SETS_BY_DATE_RANGE,
            userId,
            new Dictionary<string, object> { ["StartDate"] = startDate, ["EndDate"] = endDate, ["Page"] = page, ["PageSize"] = pageSize },
            () => ValidateDateRangeInputs(startDate, endDate, userId, page, pageSize),
            async (context) =>
            {
                var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
                var activeDataSets = dataSets.Where(ds => !ds.IsDeleted);
                
                var dateRangeResults = activeDataSets.Where(ds =>
                    ds.UploadedAt >= startDate && ds.UploadedAt <= endDate);
                
                var paginatedDataSets = ApplyPagination(dateRangeResults, page, pageSize, context);
                
                return paginatedDataSets.Select(ds => ds.ToDto());
            });
    }

    public async Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId)
    {
        return await ExecuteQueryOperationAsync(
            AppConstants.DataSetQuery.GET_DATA_SET_STATISTICS,
            userId,
            null,
            () => ValidateUserId(userId),
            async (context) =>
            {
                // Chaos engineering: Simulate cache failure during statistics calculation
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("CacheFailure", context.CorrelationId, context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating cache failure during statistics calculation", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "CacheFailure",
                        ["Operation"] = "StatisticsCalculation"
                    });
                    throw new InvalidOperationException("Simulated cache failure during statistics calculation");
                }, new Dictionary<string, object> { ["UserId"] = userId });

                var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
                var activeDataSets = dataSets.Where(ds => !ds.IsDeleted);
                var deletedDataSets = dataSets.Where(ds => ds.IsDeleted);
                
                var totalSize = activeDataSets.Sum(ds => ds.FileSize);
                var averageFileSize = activeDataSets.Any() ? activeDataSets.Average(ds => ds.FileSize) : 0;
                
                var fileTypeBreakdown = activeDataSets
                    .GroupBy(ds => ds.FileType)
                    .Select(g => new { FileType = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.FileType.ToString(), x => x.Count);
                
                var processingStatusBreakdown = activeDataSets.Any() 
                    ? new Dictionary<string, int>
                    {
                        [AppConstants.DataSetQuery.PROCESSED] = activeDataSets.Count(ds => ds.IsProcessed),
                        ["Pending"] = activeDataSets.Count(ds => !ds.IsProcessed)
                    }
                    : new Dictionary<string, int>();
                
                var recentUploads = activeDataSets
                    .OrderByDescending(ds => ds.UploadedAt)
                    .Take(AppConstants.DataSetQuery.RECENT_UPLOADS_COUNT)
                    .Select(ds => ds.ToDto());
                
                return new DataSetStatisticsDto
                {
                    TotalDataSets = activeDataSets.Count(),
                    DeletedDataSets = deletedDataSets.Count(),
                    TotalFileSize = totalSize,
                    AverageFileSize = (long)averageFileSize,
                    FileTypeBreakdown = fileTypeBreakdown,
                    ProcessingStatusBreakdown = processingStatusBreakdown,
                    RecentUploads = recentUploads
                };
            });
    }

    #region Private Helper Methods

    private async Task<T> ExecuteQueryOperationAsync<T>(
        string operationName,
        string userId,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(
            operationName,
            correlationId,
            userId,
            additionalMetadata);

        try
        {
            validation();
            
            _infrastructure.StructuredLogging.LogStep(context, $"{operationName} started");
            
            var result = await operation(context);
            
            _infrastructure.StructuredLogging.LogSummary(context, true, $"{operationName} completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogException(ex, $"{operationName} failed");
            throw;
        }
    }

    private List<DataSet> ApplyPagination(IEnumerable<DataSet> dataSets, int page, int pageSize, IOperationContext context)
    {
        // Cap page size at maximum
        var actualPageSize = Math.Min(pageSize, AppConstants.DataSetQuery.MAX_PAGE_SIZE);
        var totalCount = dataSets.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / actualPageSize);
        
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetQuery.PAGINATION_APPLIED, new Dictionary<string, object>
        {
            ["TotalCount"] = totalCount,
            ["Page"] = page,
            ["PageSize"] = actualPageSize,
            ["TotalPages"] = totalPages
        });
        
        return dataSets
            .Skip((page - 1) * actualPageSize)
            .Take(actualPageSize)
            .ToList();
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateQueryInputs(string userId, int page, int pageSize)
    {
        ValidateUserId(userId);
        ValidatePaginationInputs(page, pageSize);
    }

    private static void ValidateSearchInputs(string searchTerm, string userId, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException(AppConstants.DataSetQuery.SEARCH_TERM_CANNOT_BE_NULL_OR_EMPTY, nameof(searchTerm));
        
        if (searchTerm.Length < 3)
            throw new ArgumentException("Search term must be at least 3 characters long", nameof(searchTerm));
        
        ValidateQueryInputs(userId, page, pageSize);
    }

    private static void ValidateDateRangeInputs(DateTime startDate, DateTime endDate, string userId, int page, int pageSize)
    {
        if (startDate > endDate)
            throw new ArgumentException(AppConstants.DataSetQuery.START_DATE_CANNOT_BE_AFTER_END_DATE, nameof(startDate));
        
        if (startDate > DateTime.UtcNow)
            throw new ArgumentException("Start date cannot be in the future", nameof(startDate));
        
        ValidateQueryInputs(userId, page, pageSize);
    }

    private static void ValidatePaginationInputs(int page, int pageSize)
    {
        if (page <= 0) throw new ArgumentException(AppConstants.DataSetQuery.PAGE_MUST_BE_POSITIVE, nameof(page));
        if (pageSize <= 0) throw new ArgumentException(AppConstants.DataSetQuery.PAGE_SIZE_MUST_BE_POSITIVE, nameof(pageSize));
    }

    private static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.DataSetQuery.USER_ID_CANNOT_BE_NULL_OR_EMPTY, nameof(userId));
    }

    #endregion
} 