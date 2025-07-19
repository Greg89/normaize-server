using AutoMapper;
using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace Normaize.Core.Services;

/// <summary>
/// Service for managing data processing operations with chaos engineering resilience.
/// Implements industry-standard error handling and distributed tracing.
/// </summary>
public class DataProcessingService : IDataProcessingService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<DataProcessingService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IStructuredLoggingService _structuredLogging;
    private readonly Random _chaosRandom;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _quickTimeout = TimeSpan.FromSeconds(30);



    public DataProcessingService(
        IDataSetRepository dataSetRepository,
        IFileUploadService fileUploadService,
        IAuditService auditService,
        IMapper mapper,
        ILogger<DataProcessingService> logger,
        IMemoryCache cache,
        IStructuredLoggingService structuredLogging)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _fileUploadService = fileUploadService ?? throw new ArgumentNullException(nameof(fileUploadService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _structuredLogging = structuredLogging ?? throw new ArgumentNullException(nameof(structuredLogging));
        _chaosRandom = new Random();
    }



    public async Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(UploadDataSetAsync), 
            correlationId, 
            createDto?.UserId,
            new Dictionary<string, object>
            {
                ["FileName"] = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN
            });

        // Validate inputs first (before try-catch so exceptions are thrown)
        ValidateUploadInputs(fileRequest!, createDto!);
        
        try
        {
            // Chaos engineering: Simulate processing delay
            if (_chaosRandom.NextDouble() < 0.001) // 0.1% probability
            {
                _structuredLogging.LogStep(context, "Chaos engineering delay", new Dictionary<string, object>
                {
                    ["DelayMs"] = _chaosRandom.Next(1000, 5000)
                });
                await Task.Delay(_chaosRandom.Next(1000, 5000)); // 1-5 second delay
            }

            // Validate file
            _structuredLogging.LogStep(context, "File validation started");
            if (!await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ValidateFileAsync(fileRequest!),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_ValidateFile"))
            {
                _structuredLogging.LogStep(context, "File validation failed");
                _structuredLogging.LogSummary(context, false, "Invalid file format or size");
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = "Invalid file format or size"
                };
            }
            _structuredLogging.LogStep(context, "File validation passed");

            // Save file
            _structuredLogging.LogStep(context, "File save started");
            var filePath = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.SaveFileAsync(fileRequest!),
                _defaultTimeout,
                correlationId,
                $"{context.OperationName}_SaveFile");
            _structuredLogging.LogStep(context, "File saved", new Dictionary<string, object>
            {
                ["FilePath"] = filePath
            });

            // Process file and create dataset
            _structuredLogging.LogStep(context, "File processing started");
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest!.FileName)),
                _defaultTimeout,
                correlationId,
                $"{context.OperationName}_ProcessFile");
            _structuredLogging.LogStep(context, "File processing completed", new Dictionary<string, object>
            {
                ["RowCount"] = dataSet.RowCount,
                ["ColumnCount"] = dataSet.ColumnCount,
                ["FileSize"] = dataSet.FileSize
            });
            
            // Update with user-provided information
            dataSet.Name = createDto!.Name ?? "Unnamed Dataset";
            dataSet.Description = createDto.Description ?? string.Empty;
            dataSet.UserId = createDto.UserId ?? AppConstants.Messages.UNKNOWN;

            // Save to database
            _structuredLogging.LogStep(context, "Database save started");
            var savedDataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.AddAsync(dataSet),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_SaveToDatabase");
            context.SetMetadata("DataSetId", savedDataSet.Id);
            _structuredLogging.LogStep(context, "Database save completed");

            // Clear cache for this user
            _cache.Remove($"stats_{createDto.UserId}");

            // Log audit trail
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(
                    savedDataSet.Id,
                    createDto.UserId ?? AppConstants.Messages.UNKNOWN,
                    "Created",
                    new { 
                        fileName = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN,
                        fileSize = dataSet.FileSize,
                        rowCount = dataSet.RowCount,
                        columnCount = dataSet.ColumnCount,
                        filePath = filePath
                    }),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_AuditLog");

            _structuredLogging.LogSummary(context, true);

            return new DataSetUploadResponse
            {
                DataSetId = savedDataSet.Id,
                Success = true,
                Message = "Dataset uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            return new DataSetUploadResponse
            {
                Success = false,
                Message = "Error uploading dataset: " + ex.Message
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDataSetAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["DataSetId"] = id
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateGetDataSetInputs(id, userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Database retrieval started");
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                context.OperationName);
            _structuredLogging.LogStep(context, "Database retrieval completed");

            if (dataSet?.UserId != userId)
            {
                _structuredLogging.LogStep(context, "Access denied - user mismatch", new Dictionary<string, object>
                {
                    ["ExpectedUserId"] = userId,
                    ["ActualUserId"] = dataSet?.UserId ?? "null"
                });
                _structuredLogging.LogSummary(context, false, "Dataset not found or access denied");
                return null;
            }

            _structuredLogging.LogStep(context, "Audit logging started");
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(id, userId, "Viewed"),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_AuditLog");
            _structuredLogging.LogStep(context, "Audit logging completed");
            
            _structuredLogging.LogStep(context, "DTO mapping started");
            var result = _mapper.Map<DataSetDto>(dataSet);
            _structuredLogging.LogStep(context, "DTO mapping completed");
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for dataset ID {id}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDataSetsByUserAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["Page"] = page,
                ["PageSize"] = pageSize
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Database retrieval started");
            var dataSets = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByUserIdAsync(userId, false),
                _quickTimeout,
                correlationId,
                context.OperationName);
            _structuredLogging.LogStep(context, "Database retrieval completed", new Dictionary<string, object>
            {
                ["TotalDataSets"] = dataSets.Count()
            });

            _structuredLogging.LogStep(context, "Pagination processing started");
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            _structuredLogging.LogStep(context, "Pagination processing completed", new Dictionary<string, object>
            {
                ["PagedDataSets"] = pagedDataSets.Count
            });

            _structuredLogging.LogStep(context, "DTO mapping started");
            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            _structuredLogging.LogStep(context, "DTO mapping completed");
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for user {userId}", ex);
        }
    }

    public async Task<bool> DeleteDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(DeleteDataSetAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["DataSetId"] = id
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateDeleteInputs(id, userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            // Chaos engineering: Simulate deletion failure
            if (_chaosRandom.NextDouble() < 0.0003) // 0.03% probability
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating deletion failure", new Dictionary<string, object>
                {
                    ["ChaosType"] = "DeletionFailure",
                    ["Probability"] = 0.0003
                });
                throw new InvalidOperationException("Simulated deletion failure (chaos engineering)");
            }

            _structuredLogging.LogStep(context, "Dataset retrieval started");
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetDataSet");
            _structuredLogging.LogStep(context, "Dataset retrieval completed");

            if (dataSet == null || dataSet.UserId != userId)
            {
                _structuredLogging.LogStep(context, "Access denied - dataset not found or user mismatch", new Dictionary<string, object>
                {
                    ["ExpectedUserId"] = userId,
                    ["ActualUserId"] = dataSet?.UserId ?? "null",
                    ["DataSetFound"] = dataSet != null
                });
                _structuredLogging.LogSummary(context, false, "Dataset not found or access denied");
                return false;
            }

            // Delete the file
            if (!string.IsNullOrEmpty(dataSet.FilePath))
            {
                _structuredLogging.LogStep(context, "File deletion started", new Dictionary<string, object>
                {
                    ["FilePath"] = dataSet.FilePath
                });
                try
                {
                    await ExecuteWithTimeoutAsync(
                        () => _fileUploadService.DeleteFileAsync(dataSet.FilePath),
                        _quickTimeout,
                        correlationId,
                        $"{context.OperationName}_DeleteFile");
                    _structuredLogging.LogStep(context, "File deletion completed successfully");
                }
                catch (Exception ex)
                {
                    _structuredLogging.LogStep(context, "File deletion failed, continuing with database deletion", new Dictionary<string, object>
                    {
                        ["FileDeletionError"] = ex.Message
                    });
                }
            }
            else
            {
                _structuredLogging.LogStep(context, "No file path to delete");
            }

            _structuredLogging.LogStep(context, "Database deletion started");
            var result = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.DeleteAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_DeleteFromDatabase");
            _structuredLogging.LogStep(context, "Database deletion completed", new Dictionary<string, object>
            {
                ["DeletionResult"] = result
            });
            
            if (result)
            {
                _structuredLogging.LogStep(context, "Cache clearing started");
                _cache.Remove($"stats_{userId}");
                _structuredLogging.LogStep(context, "Cache clearing completed");

                _structuredLogging.LogStep(context, "Audit logging started");
                await ExecuteWithTimeoutAsync(
                    () => _auditService.LogDataSetActionAsync(
                        id, 
                        userId, 
                        "Deleted",
                        new { 
                            fileName = dataSet.FileName,
                            fileSize = dataSet.FileSize,
                            rowCount = dataSet.RowCount
                        }),
                    _quickTimeout,
                    correlationId,
                    $"{context.OperationName}_AuditLog");
                _structuredLogging.LogStep(context, "Audit logging completed");

                _structuredLogging.LogSummary(context, true);
            }
            else
            {
                _structuredLogging.LogSummary(context, false, "Database deletion failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for dataset ID {id}", ex);
        }
    }

    public async Task<bool> RestoreDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(RestoreDataSetAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["DataSetId"] = id
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateRestoreInputs(id, userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Dataset retrieval started");
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetDataSet");
            _structuredLogging.LogStep(context, "Dataset retrieval completed");

            if (dataSet == null || dataSet.UserId != userId)
            {
                _structuredLogging.LogStep(context, "Access denied - dataset not found or user mismatch", new Dictionary<string, object>
                {
                    ["ExpectedUserId"] = userId,
                    ["ActualUserId"] = dataSet?.UserId ?? "null",
                    ["DataSetFound"] = dataSet != null
                });
                _structuredLogging.LogSummary(context, false, "Dataset not found or access denied");
                return false;
            }

            _structuredLogging.LogStep(context, "Database restore started");
            var result = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.RestoreAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_RestoreFromDatabase");
            _structuredLogging.LogStep(context, "Database restore completed", new Dictionary<string, object>
            {
                ["RestoreResult"] = result
            });
            
            if (result)
            {
                _structuredLogging.LogStep(context, "Cache clearing started");
                _cache.Remove($"stats_{userId}");
                _structuredLogging.LogStep(context, "Cache clearing completed");

                _structuredLogging.LogStep(context, "Audit logging started");
                await ExecuteWithTimeoutAsync(
                    () => _auditService.LogDataSetActionAsync(id, userId, "Restored"),
                    _quickTimeout,
                    correlationId,
                    $"{context.OperationName}_AuditLog");
                _structuredLogging.LogStep(context, "Audit logging completed");

                _structuredLogging.LogSummary(context, true);
            }
            else
            {
                _structuredLogging.LogSummary(context, false, "Database restore failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for dataset ID {id}", ex);
        }
    }

    public async Task<bool> HardDeleteDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(HardDeleteDataSetAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["DataSetId"] = id
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateHardDeleteInputs(id, userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Dataset retrieval started");
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetDataSet");
            _structuredLogging.LogStep(context, "Dataset retrieval completed");

            if (dataSet == null || dataSet.UserId != userId)
            {
                _structuredLogging.LogStep(context, "Access denied - dataset not found or user mismatch", new Dictionary<string, object>
                {
                    ["ExpectedUserId"] = userId,
                    ["ActualUserId"] = dataSet?.UserId ?? "null",
                    ["DataSetFound"] = dataSet != null
                });
                _structuredLogging.LogSummary(context, false, "Dataset not found or access denied");
                return false;
            }

            // Delete the file
            if (!string.IsNullOrEmpty(dataSet.FilePath))
            {
                _structuredLogging.LogStep(context, "File deletion started", new Dictionary<string, object>
                {
                    ["FilePath"] = dataSet.FilePath
                });
                try
                {
                    await ExecuteWithTimeoutAsync(
                        () => _fileUploadService.DeleteFileAsync(dataSet.FilePath),
                        _quickTimeout,
                        correlationId,
                        $"{context.OperationName}_DeleteFile");
                    _structuredLogging.LogStep(context, "File deletion completed successfully");
                }
                catch (Exception ex)
                {
                    _structuredLogging.LogStep(context, "File deletion failed, continuing with database deletion", new Dictionary<string, object>
                    {
                        ["FileDeletionError"] = ex.Message
                    });
                }
            }
            else
            {
                _structuredLogging.LogStep(context, "No file path to delete");
            }

            _structuredLogging.LogStep(context, "Database hard delete started");
            var result = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.HardDeleteAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_HardDeleteFromDatabase");
            _structuredLogging.LogStep(context, "Database hard delete completed", new Dictionary<string, object>
            {
                ["HardDeleteResult"] = result
            });
            
            if (result)
            {
                _structuredLogging.LogStep(context, "Cache clearing started");
                _cache.Remove($"stats_{userId}");
                _structuredLogging.LogStep(context, "Cache clearing completed");

                _structuredLogging.LogStep(context, "Audit logging started");
                await ExecuteWithTimeoutAsync(
                    () => _auditService.LogDataSetActionAsync(
                        id, 
                        userId, 
                        "HardDeleted",
                        new { 
                            fileName = dataSet.FileName,
                            fileSize = dataSet.FileSize,
                            rowCount = dataSet.RowCount
                        }),
                    _quickTimeout,
                    correlationId,
                    $"{context.OperationName}_AuditLog");
                _structuredLogging.LogStep(context, "Audit logging completed");

                _structuredLogging.LogSummary(context, true);
            }
            else
            {
                _structuredLogging.LogSummary(context, false, "Database hard delete failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for dataset ID {id}", ex);
        }
    }

    public async Task<string?> GetDataSetPreviewAsync(int id, int rows, string userId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDataSetPreviewAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["DataSetId"] = id,
                ["RequestedRows"] = rows
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidatePreviewInputs(id, rows, userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Dataset retrieval started");
            var dataSet = await ExecuteWithTimeoutAsync<DataSet?>(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetDataSet");
            _structuredLogging.LogStep(context, "Dataset retrieval completed");

            if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.PreviewData))
            {
                _structuredLogging.LogStep(context, "Access denied or no preview data", new Dictionary<string, object>
                {
                    ["ExpectedUserId"] = userId,
                    ["ActualUserId"] = dataSet?.UserId ?? "null",
                    ["DataSetFound"] = dataSet != null,
                    ["HasPreviewData"] = !string.IsNullOrEmpty(dataSet?.PreviewData)
                });
                _structuredLogging.LogSummary(context, false, "Dataset not found, access denied, or no preview data");
                return null;
            }

            _structuredLogging.LogStep(context, "Audit logging started");
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(id, userId, "Previewed", new { rows }),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_AuditLog");
            _structuredLogging.LogStep(context, "Audit logging completed");

            _structuredLogging.LogSummary(context, true);
            return dataSet.PreviewData;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for dataset ID {id}", ex);
        }
    }

    public async Task<object?> GetDataSetSchemaAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDataSetSchemaAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["DataSetId"] = id
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateSchemaInputs(id, userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Dataset retrieval started");
            var dataSet = await ExecuteWithTimeoutAsync<DataSet?>(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetDataSet");
            _structuredLogging.LogStep(context, "Dataset retrieval completed");

            if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.Schema))
            {
                _structuredLogging.LogStep(context, "Access denied or no schema", new Dictionary<string, object>
                {
                    ["ExpectedUserId"] = userId,
                    ["ActualUserId"] = dataSet?.UserId ?? "null",
                    ["DataSetFound"] = dataSet != null,
                    ["HasSchema"] = !string.IsNullOrEmpty(dataSet?.Schema)
                });
                _structuredLogging.LogSummary(context, false, "Dataset not found, access denied, or no schema");
                return null;
            }

            _structuredLogging.LogStep(context, "Schema deserialization started");
            var schema = await DeserializeSchemaSafelyAsync(dataSet.Schema, id, correlationId);
            _structuredLogging.LogStep(context, "Schema deserialization completed");
            
            _structuredLogging.LogSummary(context, true);
            return schema;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for dataset ID {id}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDeletedDataSetsAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["Page"] = page,
                ["PageSize"] = pageSize
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Database retrieval started");
            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetByUserIdAsync(userId, includeDeleted: true),
                _quickTimeout,
                correlationId,
                context.OperationName);
            _structuredLogging.LogStep(context, "Database retrieval completed", new Dictionary<string, object>
            {
                ["TotalDataSets"] = dataSets.Count()
            });

            _structuredLogging.LogStep(context, "Filtering deleted datasets started");
            var deletedDataSets = dataSets
                .Where(d => d.IsDeleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            _structuredLogging.LogStep(context, "Filtering deleted datasets completed", new Dictionary<string, object>
            {
                ["DeletedDataSets"] = deletedDataSets.Count
            });

            _structuredLogging.LogStep(context, "DTO mapping started");
            var result = _mapper.Map<IEnumerable<DataSetDto>>(deletedDataSets);
            _structuredLogging.LogStep(context, "DTO mapping completed");
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for user {userId}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(SearchDataSetsAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["SearchTerm"] = searchTerm,
                ["Page"] = page,
                ["PageSize"] = pageSize
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateSearchInputs(searchTerm, userId, page, pageSize);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Search operation started");
            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.SearchAsync(searchTerm, userId),
                _quickTimeout,
                correlationId,
                context.OperationName);
            _structuredLogging.LogStep(context, "Search operation completed", new Dictionary<string, object>
            {
                ["TotalMatches"] = dataSets.Count()
            });

            _structuredLogging.LogStep(context, "Pagination processing started");
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            _structuredLogging.LogStep(context, "Pagination processing completed", new Dictionary<string, object>
            {
                ["PagedResults"] = pagedDataSets.Count
            });

            _structuredLogging.LogStep(context, "DTO mapping started");
            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            _structuredLogging.LogStep(context, "DTO mapping completed");
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for user {userId} with search term '{searchTerm}'", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(FileType fileType, string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDataSetsByFileTypeAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["FileType"] = fileType.ToString(),
                ["Page"] = page,
                ["PageSize"] = pageSize
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Database retrieval by file type started");
            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetByFileTypeAsync(fileType, userId),
                _quickTimeout,
                correlationId,
                context.OperationName);
            _structuredLogging.LogStep(context, "Database retrieval by file type completed", new Dictionary<string, object>
            {
                ["TotalDataSets"] = dataSets.Count()
            });

            _structuredLogging.LogStep(context, "Pagination processing started");
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            _structuredLogging.LogStep(context, "Pagination processing completed", new Dictionary<string, object>
            {
                ["PagedDataSets"] = pagedDataSets.Count
            });

            _structuredLogging.LogStep(context, "DTO mapping started");
            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            _structuredLogging.LogStep(context, "DTO mapping completed");
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for file type {fileType}, user {userId}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDataSetsByDateRangeAsync), 
            correlationId, 
            userId,
            new Dictionary<string, object>
            {
                ["StartDate"] = startDate.ToString("yyyy-MM-dd"),
                ["EndDate"] = endDate.ToString("yyyy-MM-dd"),
                ["Page"] = page,
                ["PageSize"] = pageSize
            });

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateDateRangeInputs(startDate, endDate, userId, page, pageSize);
            _structuredLogging.LogStep(context, "Input validation completed");

            _structuredLogging.LogStep(context, "Database retrieval by date range started");
            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetByDateRangeAsync(startDate, endDate, userId),
                _quickTimeout,
                correlationId,
                context.OperationName);
            _structuredLogging.LogStep(context, "Database retrieval by date range completed", new Dictionary<string, object>
            {
                ["TotalDataSets"] = dataSets.Count()
            });

            _structuredLogging.LogStep(context, "Pagination processing started");
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            _structuredLogging.LogStep(context, "Pagination processing completed", new Dictionary<string, object>
            {
                ["PagedDataSets"] = pagedDataSets.Count
            });

            _structuredLogging.LogStep(context, "DTO mapping started");
            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            _structuredLogging.LogStep(context, "DTO mapping completed");
            
            _structuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for date range, user {userId}", ex);
        }
    }

    public async Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId)
    {
        var correlationId = GetCorrelationId();
        var context = _structuredLogging.CreateContext(
            nameof(GetDataSetStatisticsAsync), 
            correlationId, 
            userId);

        try
        {
            _structuredLogging.LogStep(context, "Input validation started");
            ValidateUserId(userId);
            _structuredLogging.LogStep(context, "Input validation completed");

            // Chaos engineering: Simulate cache corruption
            if (_chaosRandom.NextDouble() < 0.0005) // 0.05% probability
            {
                _structuredLogging.LogStep(context, "Chaos engineering: Simulating cache corruption", new Dictionary<string, object>
                {
                    ["ChaosType"] = "CacheCorruption",
                    ["Probability"] = 0.0005
                });
                _cache.Remove($"stats_{userId}");
            }

            var cacheKey = $"stats_{userId}";
            
            _structuredLogging.LogStep(context, "Cache lookup started");
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out DataSetStatisticsDto? cachedStats))
            {
                _structuredLogging.LogStep(context, "Statistics retrieved from cache");
                _structuredLogging.LogSummary(context, true);
                return cachedStats!;
            }
            _structuredLogging.LogStep(context, "Cache miss - calculating statistics");

            _structuredLogging.LogStep(context, "Total count calculation started");
            var totalCount = await ExecuteWithTimeoutAsync<int>(
                () => _dataSetRepository.GetTotalCountAsync(userId),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetTotalCount");
            _structuredLogging.LogStep(context, "Total count calculation completed", new Dictionary<string, object>
            {
                ["TotalCount"] = totalCount
            });

            _structuredLogging.LogStep(context, "Total size calculation started");
            var totalSize = await ExecuteWithTimeoutAsync<long>(
                () => _dataSetRepository.GetTotalSizeAsync(userId),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetTotalSize");
            _structuredLogging.LogStep(context, "Total size calculation completed", new Dictionary<string, object>
            {
                ["TotalSize"] = totalSize
            });

            _structuredLogging.LogStep(context, "Recently modified retrieval started");
            var recentlyModified = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetRecentlyModifiedAsync(userId, 5),
                _quickTimeout,
                correlationId,
                $"{context.OperationName}_GetRecentlyModified");
            _structuredLogging.LogStep(context, "Recently modified retrieval completed", new Dictionary<string, object>
            {
                ["RecentlyModifiedCount"] = recentlyModified.Count()
            });

            _structuredLogging.LogStep(context, "Statistics object creation started");
            var statistics = new DataSetStatisticsDto
            {
                TotalCount = totalCount,
                TotalSize = totalSize,
                RecentlyModified = _mapper.Map<IEnumerable<DataSetDto>>(recentlyModified)
            };
            _structuredLogging.LogStep(context, "Statistics object creation completed");

            _structuredLogging.LogStep(context, "Cache storage started");
            _cache.Set(cacheKey, statistics, _cacheExpiration);
            _structuredLogging.LogStep(context, "Cache storage completed");

            _structuredLogging.LogSummary(context, true);
            return statistics;
        }
        catch (Exception ex)
        {
            _structuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {context.OperationName} for user {userId}", ex);
        }
    }

    #region Private Methods

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private async Task ExecuteWithTimeoutAsync(Func<Task> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private async Task<object?> DeserializeSchemaSafelyAsync(string schema, int dataSetId, string correlationId)
    {
        try
        {
            return await Task.Run(() => JsonSerializer.Deserialize<object>(schema));
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Failed to deserialize schema for dataset {DataSetId}. CorrelationId: {CorrelationId}", 
                dataSetId, correlationId);
            return null;
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateUploadInputs(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        ArgumentNullException.ThrowIfNull(fileRequest);
        ArgumentNullException.ThrowIfNull(createDto);

        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException("File name is required", nameof(fileRequest));

        if (string.IsNullOrWhiteSpace(createDto.Name))
            throw new ArgumentException("Dataset name is required", nameof(createDto));

        if (string.IsNullOrWhiteSpace(createDto.UserId))
            throw new ArgumentException("User ID is required", nameof(createDto));

        // Security: Validate file path to prevent directory traversal
        if (fileRequest.FileName.Contains("..") || fileRequest.FileName.Contains("/") || fileRequest.FileName.Contains("\\"))
            throw new ArgumentException("Invalid file name", nameof(fileRequest));
    }

    private static void ValidateGetDataSetInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateDeleteInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateRestoreInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateHardDeleteInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidatePreviewInputs(int id, int rows, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        if (rows <= 0 || rows > 1000)
            throw new ArgumentException("Rows must be between 1 and 1000", nameof(rows));
        
        ValidateUserId(userId);
    }

    private static void ValidateSchemaInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateSearchInputs(string searchTerm, string userId, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term is required", nameof(searchTerm));
        
        if (searchTerm.Length > 100)
            throw new ArgumentException("Search term cannot exceed 100 characters", nameof(searchTerm));
        
        ValidateUserId(userId);
        ValidatePaginationInputs(page, pageSize);
    }

    private static void ValidateDateRangeInputs(DateTime startDate, DateTime endDate, string userId, int page, int pageSize)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");
        
        if (endDate > DateTime.UtcNow.AddDays(1))
            throw new ArgumentException("End date cannot be in the future");
        
        if (startDate < DateTime.UtcNow.AddYears(-10))
            throw new ArgumentException("Start date cannot be more than 10 years ago");
        
        ValidateUserId(userId);
        ValidatePaginationInputs(page, pageSize);
    }

    private static void ValidatePaginationInputs(int page, int pageSize)
    {
        if (page <= 0)
            throw new ArgumentException("Page must be positive", nameof(page));
        
        if (pageSize <= 0 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
    }

    private static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        
        if (userId.Length > 100)
            throw new ArgumentException("User ID cannot exceed 100 characters", nameof(userId));
    }

    #endregion
} 