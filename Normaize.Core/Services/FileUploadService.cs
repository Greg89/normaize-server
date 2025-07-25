using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using OfficeOpenXml;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using System.Diagnostics;

namespace Normaize.Core.Services;

public class FileUploadService : IFileUploadService
{
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly DataProcessingConfiguration _dataProcessingConfig;
    private readonly IStorageService _storageService;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public FileUploadService(
        IOptions<FileUploadConfiguration> fileUploadConfig,
        IOptions<DataProcessingConfiguration> dataProcessingConfig,
        IStorageService storageService,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(fileUploadConfig);
        ArgumentNullException.ThrowIfNull(dataProcessingConfig);
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(infrastructure);
        _fileUploadConfig = fileUploadConfig.Value;
        _dataProcessingConfig = dataProcessingConfig.Value;
        _storageService = storageService;
        _infrastructure = infrastructure;

        ValidateConfiguration();
        LogConfiguration();
    }

    private void ValidateConfiguration()
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(_fileUploadConfig);

        if (!Validator.TryValidateObject(_fileUploadConfig, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"{AppConstants.FileUploadMessages.CONFIGURATION_VALIDATION_FAILED}: {errors}");
        }

        validationResults.Clear();
        validationContext = new ValidationContext(_dataProcessingConfig);

        if (!Validator.TryValidateObject(_dataProcessingConfig, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"{AppConstants.FileUploadMessages.CONFIGURATION_VALIDATION_FAILED}: {errors}");
        }

        // Additional cross-validation
        if (_fileUploadConfig.AllowedExtensions.Any(ext => _fileUploadConfig.BlockedExtensions.Contains(ext)))
        {
            throw new InvalidOperationException(AppConstants.FileUploadMessages.ALLOWED_EXTENSIONS_CONFLICT);
        }
    }

    private void LogConfiguration()
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(
            "LogConfiguration",
            correlationId,
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["MaxFileSizeMB"] = _fileUploadConfig.MaxFileSize / AppConstants.FileProcessing.BYTES_PER_MEGABYTE,
                ["MaxRowsPerDataset"] = _dataProcessingConfig.MaxRowsPerDataset,
                ["AllowedExtensions"] = string.Join(", ", _fileUploadConfig.AllowedExtensions),
                ["BlockedExtensions"] = string.Join(", ", _fileUploadConfig.BlockedExtensions)
            });

        _infrastructure.StructuredLogging.LogStep(context, "FileUploadService configuration logged");
        _infrastructure.StructuredLogging.LogSummary(context, true);
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        return await ExecuteFileOperationAsync(
            operationName: nameof(SaveFileAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN,
                ["FileSize"] = fileRequest?.FileSize ?? 0
            },
            validation: () => ValidateFileUploadRequest(fileRequest!),
            operation: async (context) =>
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_UPLOAD_STARTED);

                // Apply chaos engineering for file upload
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(
                    AppConstants.FileProcessing.STORAGE_FAILURE_SCENARIO,
                    GetCorrelationId(),
                    context.OperationName,
                    async () => await Task.Delay(AppConstants.FileUpload.FILE_UPLOAD_CHAOS_DELAY_MS),
                    new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest!.FileName });

                // Validate file before saving
                var isValid = await ValidateFileAsync(fileRequest!);
                if (!isValid)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_FAILED);
                    throw new FileValidationException(string.Format(AppConstants.FileUpload.FILE_VALIDATION_FAILED_ERROR, fileRequest!.FileName));
                }

                try
                {
                    var filePath = await _storageService.SaveFileAsync(fileRequest!);

                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_UPLOAD_SUCCESS);

                    return filePath;
                }
                catch (Exception ex)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_UPLOAD_FAILED);
                    throw new FileUploadException(string.Format(AppConstants.FileUpload.FAILED_SAVE_FILE_ERROR, fileRequest!.FileName), ex);
                }
            });
    }

    public async Task<bool> ValidateFileAsync(FileUploadRequest fileRequest)
    {
        return await ExecuteFileOperationAsync(
            operationName: nameof(ValidateFileAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN,
                ["FileSize"] = fileRequest?.FileSize ?? 0
            },
            validation: () => ValidateFileUploadRequest(fileRequest!),
            operation: (context) =>
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_STARTED);

                if (!IsFileSizeValid(fileRequest!.FileSize, context))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_SIZE_VALIDATION_FAILED);
                    return Task.FromResult(false);
                }

                var fileExtension = GetFileExtension(fileRequest.FileName);

                if (!IsFileExtensionValid(fileExtension, context))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_EXTENSION_VALIDATION_FAILED);
                    return Task.FromResult(false);
                }

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_PASSED);

                return Task.FromResult(true);
            });
    }

    public async Task<DataSet> ProcessFileAsync(string filePath, string fileType)
    {
        return await ExecuteFileOperationAsync(
            operationName: nameof(ProcessFileAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath,
                ["FileType"] = fileType
            },
            validation: () => ValidateFileProcessingInputs(filePath, fileType),
            operation: async (context) =>
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_PROCESSING_STARTED);

                // Apply chaos engineering for file processing
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(
                    AppConstants.FileProcessing.PROCESSING_DELAY_SCENARIO,
                    GetCorrelationId(),
                    context.OperationName,
                    async () => await Task.Delay(AppConstants.FileUpload.FILE_PROCESSING_CHAOS_DELAY_MS),
                    new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_TYPE_KEY] = fileType });

                // Validate file exists
                await ValidateFileExistsAsync(filePath, context);

                var dataSet = CreateInitialDataSet(filePath, fileType);

                await ProcessFileByTypeAsync(filePath, fileType, dataSet, context);
                await FinalizeDataSetAsync(filePath, dataSet);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_PROCESSED_SUCCESS);

                return dataSet;
            });
    }

    public async Task DeleteFileAsync(string filePath)
    {
        await ExecuteFileOperationAsync(
            operationName: nameof(DeleteFileAsync),
            additionalMetadata: new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath },
            validation: () => ValidateFilePath(filePath),
            operation: async (context) =>
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_DELETION_STARTED);

                // Apply chaos engineering for file deletion
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(
                    AppConstants.FileProcessing.STORAGE_FAILURE_SCENARIO,
                    GetCorrelationId(),
                    context.OperationName,
                    async () => await Task.Delay(AppConstants.FileUpload.FILE_DELETION_CHAOS_DELAY_MS),
                    new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath });

                try
                {
                    await _storageService.DeleteFileAsync(filePath);

                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_DELETED_SUCCESS);
                }
                catch (Exception)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_DELETION_FAILED);
                    // Don't re-throw - log and continue (as per original behavior)
                }

                return true; // Return value for consistency
            });
    }

    #region Private Methods

    private async Task<T> ExecuteFileOperationAsync<T>(
        string operationName,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(operationName, correlationId, AppConstants.Auth.AnonymousUser, additionalMetadata);

        try
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            validation();
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            var result = await operation(context);
            _infrastructure.StructuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogSummary(context, false, ex.Message);

            // Preserve specific exception types for better error handling
            if (ex is FileNotFoundException || ex is UnsupportedFileTypeException ||
                ex is FileUploadException || ex is FileProcessingException ||
                ex is FileValidationException)
            {
                throw; // Re-throw specific exceptions as-is
            }

            // Create detailed error message based on operation type and metadata
            var errorMessage = CreateDetailedErrorMessage(operationName, additionalMetadata);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private static string CreateDetailedErrorMessage(string operationName, Dictionary<string, object>? metadata)
    {
        if (metadata == null) return $"Failed to complete {operationName}";

        // Handle specific operation types with detailed error messages
        switch (operationName)
        {
            case nameof(SaveFileAsync):
                var fileName = metadata.TryGetValue(AppConstants.FileProcessing.FILE_NAME_KEY, out var name) ? name?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for file '{fileName}'";

            case nameof(ValidateFileAsync):
                var validateFileName = metadata.TryGetValue(AppConstants.FileProcessing.FILE_NAME_KEY, out var validateName) ? validateName?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for file '{validateFileName}'";

            case nameof(ProcessFileAsync):
                var filePath = metadata.TryGetValue(AppConstants.FileProcessing.FILE_PATH_KEY, out var path) ? path?.ToString() : AppConstants.Messages.UNKNOWN;
                var fileType = metadata.TryGetValue("FileType", out var type) ? type?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for file '{filePath}' of type '{fileType}'";

            case nameof(DeleteFileAsync):
                var deleteFilePath = metadata.TryGetValue(AppConstants.FileProcessing.FILE_PATH_KEY, out var deletePath) ? deletePath?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for file '{deleteFilePath}'";

            default:
                return $"Failed to complete {operationName}";
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateFileUploadRequest(FileUploadRequest fileRequest)
    {
        ArgumentNullException.ThrowIfNull(fileRequest);

        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException(AppConstants.FileUpload.FILE_NAME_REQUIRED, nameof(fileRequest));

        if (fileRequest.FileSize <= 0)
            throw new ArgumentException(AppConstants.FileUpload.FILE_SIZE_MUST_BE_POSITIVE, nameof(fileRequest));
    }

    private static void ValidateFileProcessingInputs(string filePath, string fileType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(AppConstants.FileUpload.FILE_PATH_REQUIRED, nameof(filePath));

        if (string.IsNullOrWhiteSpace(fileType))
            throw new ArgumentException(AppConstants.FileUpload.FILE_TYPE_REQUIRED, nameof(fileType));
    }

    private static void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(AppConstants.FileUpload.FILE_PATH_REQUIRED, nameof(filePath));
    }

    #endregion

    #region File Processing Methods

    private bool IsFileSizeValid(long fileSize, IOperationContext context)
    {
        if (fileSize <= _fileUploadConfig.MaxFileSize) return true;

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_SIZE_EXCEEDS_LIMIT_WARNING, new Dictionary<string, object>
        {
            ["FileSize"] = fileSize,
            ["MaxSize"] = _fileUploadConfig.MaxFileSize
        });
        return false;
    }

    private static string GetFileExtension(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant();

    private bool IsFileExtensionValid(string fileExtension, IOperationContext context)
    {
        // Check if extension is blocked
        if (_fileUploadConfig.BlockedExtensions.Contains(fileExtension))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_EXTENSION_BLOCKED_WARNING, new Dictionary<string, object>
            {
                ["Extension"] = fileExtension
            });
            return false;
        }

        // Check if extension is allowed
        if (!_fileUploadConfig.AllowedExtensions.Contains(fileExtension))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_EXTENSION_NOT_ALLOWED_WARNING, new Dictionary<string, object>
            {
                ["Extension"] = fileExtension,
                ["AllowedExtensions"] = string.Join(", ", _fileUploadConfig.AllowedExtensions)
            });
            return false;
        }

        return true;
    }

    private async Task ValidateFileExistsAsync(string filePath, IOperationContext context)
    {
        var fileExists = await _storageService.FileExistsAsync(filePath);
        if (!fileExists)
        {
            var error = string.Format(AppConstants.FileUpload.FILE_NOT_FOUND_ERROR, filePath);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_NOT_FOUND_PROCESSING_WARNING, new Dictionary<string, object>
            {
                ["FilePath"] = filePath
            });
            throw new FileNotFoundException(error);
        }
    }

    private static DataSet CreateInitialDataSet(string filePath, string fileType) => new()
    {
        FileName = Path.GetFileName(filePath),
        FilePath = filePath,
        FileType = GetFileTypeFromExtension(fileType),
        FileSize = 0, // Will be calculated during processing
        UploadedAt = DateTime.UtcNow,
        StorageProvider = GetStorageProviderFromPath(filePath)
    };

    private async Task ProcessFileByTypeAsync(string filePath, string fileType, DataSet dataSet, IOperationContext context)
    {
        var processor = GetFileProcessor(fileType);
        await processor(filePath, dataSet, context);
    }

    private delegate Task FileProcessor(string filePath, DataSet dataSet, IOperationContext context);

    private FileProcessor GetFileProcessor(string fileType) => fileType.ToLowerInvariant() switch
    {
        ".csv" => ProcessCsvFileAsync,
        ".json" => ProcessJsonFileAsync,
        ".xlsx" or ".xls" => ProcessExcelFileAsync,
        ".xml" => ProcessXmlFileAsync,
        ".txt" => ProcessTextFileAsync,
        _ => throw new UnsupportedFileTypeException(string.Format(AppConstants.FileUpload.UNSUPPORTED_FILE_TYPE_ERROR, fileType))
    };

    private async Task FinalizeDataSetAsync(string filePath, DataSet dataSet)
    {
        // Generate data hash for change detection
        dataSet.DataHash = await GenerateDataHashAsync(filePath);

        // Determine storage strategy
        dataSet.UseSeparateTable = ShouldUseSeparateTable(dataSet);

        // Only mark as processed if there are no errors
        if (string.IsNullOrEmpty(dataSet.ProcessingErrors))
        {
            dataSet.IsProcessed = true;
            dataSet.ProcessedAt = DateTime.UtcNow;
        }
    }

    private bool ShouldUseSeparateTable(DataSet dataSet) =>
        dataSet.RowCount >= _dataProcessingConfig.MaxRowsPerDataset ||
        dataSet.FileSize > _fileUploadConfig.MaxFileSize;

    private void HandleProcessingError(string filePath, string fileType, DataSet dataSet, IOperationContext context, Exception ex)
    {
        var error = string.Format(AppConstants.FileUpload.ERROR_PROCESSING_FILE, filePath, ex.Message);
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.UNEXPECTED_ERROR_FILE_PROCESSING, new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["FileType"] = fileType,
            ["ErrorMessage"] = ex.Message
        });

        dataSet.IsProcessed = false;
        dataSet.ProcessingErrors = error;
    }

    #endregion

    #region File Type Processing Methods

    private async Task ProcessCsvFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            using var csv = CreateCsvReader(reader);

            var (headers, records) = await ExtractCsvDataAsync(csv, context, filePath);
            var limitedHeaders = LimitColumns(headers, context, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, context, filePath);
        }
        catch (CsvHelperException ex)
        {
            HandleCsvProcessingError(context, filePath, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(context, filePath, ex, AppConstants.FileProcessing.CSV_FILE_TYPE);
        }
        catch (Exception ex)
        {
            HandleProcessingError(filePath, ".csv", dataSet, context, ex);
        }
    }

    private static CsvReader CreateCsvReader(StreamReader reader) => new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null,
        Delimiter = AppConstants.FileProcessing.DEFAULT_DELIMITER,
        HasHeaderRecord = true
    });

    private async Task<(List<string> Headers, List<Dictionary<string, object>> Records)> ExtractCsvDataAsync(
        CsvReader csv, IOperationContext context, string filePath)
    {
        var records = new List<Dictionary<string, object>>();
        var headers = new List<string>();

        if (await csv.ReadAsync())
        {
            csv.ReadHeader();
            headers = csv.HeaderRecord?.ToList() ?? [];

            if (headers.Count == 0)
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.CSV_NO_HEADERS_WARNING, new Dictionary<string, object>
                {
                    ["FilePath"] = filePath
                });
            }
        }

        var rowCount = 0;
        var maxRows = _dataProcessingConfig.MaxRowsPerDataset;

        // Pre-allocate capacity for better performance
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY); // Reasonable initial capacity

        while (await csv.ReadAsync() && rowCount < maxRows)
        {
            var record = new Dictionary<string, object>(headers.Count); // Pre-allocate dictionary size
            foreach (var header in headers)
            {
                var field = csv.GetField(header);
                record[header] = field ?? string.Empty; // Avoid boxing by using string.Empty
            }
            records.Add(record);
            rowCount++;
        }

        return (headers, records);
    }

    private List<string> LimitColumns(List<string> headers, IOperationContext context, string filePath)
    {
        if (headers.Count <= _dataProcessingConfig.MaxColumnsPerDataset)
            return headers;

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_TOO_MANY_COLUMNS_WARNING, new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["ColumnCount"] = headers.Count,
            ["MaxColumns"] = _dataProcessingConfig.MaxColumnsPerDataset
        });
        return headers.Take(_dataProcessingConfig.MaxColumnsPerDataset).ToList();
    }

    private void PopulateDataSet(DataSet dataSet, List<string> headers, List<Dictionary<string, object>> records,
        long fileSize, IOperationContext context, string filePath)
    {
        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = records.Count;
        dataSet.FileSize = fileSize;

        // Optimize JSON serialization with reusable options
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false, // Smaller output
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        dataSet.Schema = JsonSerializer.Serialize(headers, jsonOptions);

        // Only serialize preview data if needed
        if (records.Count > 0)
        {
            var previewRecords = records.Take(_dataProcessingConfig.MaxPreviewRows).ToList();
            dataSet.PreviewData = JsonSerializer.Serialize(previewRecords, jsonOptions);
        }

        // Only serialize full data if within limits
        if (records.Count < _dataProcessingConfig.MaxRowsPerDataset)
        {
            dataSet.ProcessedData = JsonSerializer.Serialize(records, jsonOptions);
        }

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_PROCESSING_COMPLETED_DEBUG, new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["RowCount"] = records.Count,
            ["ColumnCount"] = headers.Count
        });
    }

    private void HandleCsvProcessingError(IOperationContext context, string filePath, CsvHelperException ex)
    {
        // Use string interpolation for better performance
        var error = string.Format(AppConstants.FileUpload.CSV_PARSING_ERROR, ex.Message);
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.CSV_PARSING_FAILED_ERROR, new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["ErrorMessage"] = ex.Message
        });
        throw new FileProcessingException(error, ex);
    }

    private void HandleJsonSerializationError(IOperationContext context, string filePath, JsonException ex, string fileType)
    {
        // Use string interpolation for better performance
        var error = string.Format(AppConstants.FileUpload.JSON_SERIALIZATION_ERROR, fileType, ex.Message);
        _infrastructure.StructuredLogging.LogStep(context, string.Format(AppConstants.FileUpload.JSON_SERIALIZATION_FAILED_ERROR, fileType), new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["FileType"] = fileType,
            ["ErrorMessage"] = ex.Message
        });
        throw new FileProcessingException(error, ex);
    }

    private async Task ProcessJsonFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var jsonContent = await reader.ReadToEndAsync();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            var (headers, records) = ExtractJsonData(jsonElement, context, filePath);
            var limitedHeaders = LimitColumns(headers.ToList(), context, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, context, filePath);
        }
        catch (JsonException ex)
        {
            var error = string.Format(AppConstants.FileUpload.JSON_PARSING_ERROR, ex.Message);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.JSON_PARSING_FAILED_ERROR, new Dictionary<string, object>
            {
                ["FilePath"] = filePath,
                ["ErrorMessage"] = ex.Message
            });
            throw new FileProcessingException(error, ex);
        }
    }

    private (HashSet<string> Headers, List<Dictionary<string, object>> Records) ExtractJsonData(
        JsonElement jsonElement, IOperationContext context, string filePath)
    {
        var records = new List<Dictionary<string, object>>();
        var headers = new HashSet<string>();

        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Array:
                ExtractJsonArrayData(jsonElement, headers, records);
                break;
            case JsonValueKind.Object:
                ExtractJsonObjectData(jsonElement, headers, records);
                break;
            default:
                var error = string.Format(AppConstants.FileUpload.UNSUPPORTED_JSON_STRUCTURE, jsonElement.ValueKind);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.UNSUPPORTED_JSON_STRUCTURE_WARNING, new Dictionary<string, object>
                {
                    ["FilePath"] = filePath,
                    ["ValueKind"] = jsonElement.ValueKind
                });
                throw new FileProcessingException(error);
        }

        return (headers, records);
    }

    private void ExtractJsonArrayData(JsonElement jsonElement, HashSet<string> headers, List<Dictionary<string, object>> records)
    {
        var maxRows = _dataProcessingConfig.MaxRowsPerDataset;
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY); // Pre-allocate capacity

        foreach (var item in jsonElement.EnumerateArray().Take(maxRows))
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                var record = new Dictionary<string, object>();
                foreach (var property in item.EnumerateObject())
                {
                    headers.Add(property.Name);
                    record[property.Name] = property.Value.ToString();
                }
                records.Add(record);
            }
        }
    }

    private static void ExtractJsonObjectData(JsonElement jsonElement, HashSet<string> headers, List<Dictionary<string, object>> records)
    {
        // Single object - convert to array format
        var record = new Dictionary<string, object>();
        foreach (var property in jsonElement.EnumerateObject())
        {
            headers.Add(property.Name);
            record[property.Name] = property.Value.ToString();
        }
        records.Add(record);
    }

    private async Task ProcessExcelFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var package = new ExcelPackage(fileStream);
            var worksheet = GetWorksheet(package);

            var headers = ExtractExcelHeaders(worksheet);
            var limitedHeaders = LimitColumns(headers, context, filePath);
            var records = ExtractExcelData(worksheet, limitedHeaders);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, context, filePath);
        }
        catch (InvalidOperationException ex)
        {
            var error = string.Format(AppConstants.FileUpload.EXCEL_PROCESSING_ERROR, ex.Message);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.EXCEL_PROCESSING_FAILED_ERROR, new Dictionary<string, object>
            {
                ["FilePath"] = filePath,
                ["ErrorMessage"] = ex.Message
            });
            throw new FileProcessingException(error, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(context, filePath, ex, AppConstants.FileProcessing.EXCEL_FILE_TYPE);
        }
    }

    private static ExcelWorksheet GetWorksheet(ExcelPackage package)
    {
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet != null) return worksheet;

        var error = AppConstants.FileUpload.NO_WORKSHEET_FOUND;
        throw new InvalidOperationException(error);
    }

    private static List<string> ExtractExcelHeaders(ExcelWorksheet worksheet)
    {
        var headers = new List<string>();
        var headerRow = worksheet.Cells[AppConstants.FileProcessing.HEADER_ROW_INDEX, AppConstants.FileProcessing.DEFAULT_COLUMN_INDEX, AppConstants.FileProcessing.HEADER_ROW_INDEX, worksheet.Dimension?.Columns ?? AppConstants.FileProcessing.DEFAULT_COLUMN_INDEX];

        foreach (var cell in headerRow)
        {
            headers.Add(cell.Value?.ToString() ?? $"{AppConstants.FileProcessing.DEFAULT_COLUMN_PREFIX}{cell.Start.Column}");
        }

        return headers;
    }

    private List<Dictionary<string, object>> ExtractExcelData(ExcelWorksheet worksheet, List<string> headers)
    {
        var records = new List<Dictionary<string, object>>();
        var rowCount = 0;
        var maxRows = _dataProcessingConfig.MaxRowsPerDataset;
        var maxCols = headers.Count;

        // Pre-allocate capacity for better performance
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY);

        // Optimize by reading entire range at once when possible
        var dataRange = worksheet.Cells[AppConstants.FileProcessing.DATA_START_ROW_INDEX, AppConstants.FileProcessing.DEFAULT_COLUMN_INDEX,
            Math.Min(worksheet.Dimension?.Rows ?? AppConstants.FileProcessing.DEFAULT_COLUMN_INDEX, AppConstants.FileProcessing.DATA_START_ROW_INDEX + maxRows - 1),
            maxCols];

        var dataValues = dataRange.Value as object[,];

        if (dataValues != null)
        {
            // Process data as 2D array for better performance
            for (int row = 0; row < dataValues.GetLength(0) && rowCount < maxRows; row++)
            {
                var record = new Dictionary<string, object>(maxCols);
                for (int col = 0; col < maxCols; col++)
                {
                    var cellValue = dataValues[row, col];
                    record[headers[col]] = cellValue?.ToString() ?? string.Empty;
                }
                records.Add(record);
                rowCount++;
            }
        }
        else
        {
            // Fallback to cell-by-cell access if range reading fails
            for (int row = AppConstants.FileProcessing.DATA_START_ROW_INDEX; row <= (worksheet.Dimension?.Rows ?? AppConstants.FileProcessing.DEFAULT_COLUMN_INDEX) && rowCount < maxRows; row++)
            {
                var record = new Dictionary<string, object>(maxCols);
                for (int col = AppConstants.FileProcessing.DEFAULT_COLUMN_INDEX; col <= maxCols; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    record[headers[col - AppConstants.FileProcessing.DEFAULT_COLUMN_INDEX]] = cellValue?.ToString() ?? string.Empty;
                }
                records.Add(record);
                rowCount++;
            }
        }

        return records;
    }

    private async Task ProcessXmlFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var xmlContent = await reader.ReadToEndAsync();
            var doc = XDocument.Parse(xmlContent);

            var (headers, records) = ExtractXmlData(doc);
            var limitedHeaders = LimitColumns(headers.ToList(), context, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, context, filePath);
        }
        catch (XmlException ex)
        {
            var error = string.Format(AppConstants.FileUpload.XML_PARSING_ERROR, ex.Message);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.XML_PARSING_FAILED_ERROR, new Dictionary<string, object>
            {
                ["FilePath"] = filePath,
                ["ErrorMessage"] = ex.Message
            });
            throw new FileProcessingException(error, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(context, filePath, ex, AppConstants.FileProcessing.XML_FILE_TYPE);
        }
    }

    private (HashSet<string> Headers, List<Dictionary<string, object>> Records) ExtractXmlData(XDocument doc)
    {
        var records = new List<Dictionary<string, object>>();
        var headers = new HashSet<string>();

        var root = doc.Root;
        if (root == null) return (headers, records);

        var children = root.Elements().ToList();
        if (children.Count > 0)
        {
            ExtractXmlArrayData(children, headers, records);
        }
        else
        {
            ExtractXmlObjectData(root, headers, records);
        }

        return (headers, records);
    }

    private void ExtractXmlArrayData(List<XElement> children, HashSet<string> headers, List<Dictionary<string, object>> records)
    {
        var maxRows = _dataProcessingConfig.MaxRowsPerDataset;
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY); // Pre-allocate capacity

        // Assume first child is the template for data rows
        var firstChild = children.First();
        var childElements = firstChild.Elements().ToList();

        // Extract headers from first element
        foreach (var element in childElements)
        {
            headers.Add(element.Name.LocalName);
        }

        // Process all child elements as data rows
        foreach (var child in children.Take(maxRows))
        {
            var record = new Dictionary<string, object>(headers.Count);
            foreach (var element in child.Elements())
            {
                record[element.Name.LocalName] = element.Value ?? string.Empty;
            }
            records.Add(record);
        }
    }

    private static void ExtractXmlObjectData(XElement root, HashSet<string> headers, List<Dictionary<string, object>> records)
    {
        // Single object - convert attributes and elements to record
        var record = new Dictionary<string, object>();
        foreach (var attr in root.Attributes())
        {
            headers.Add(attr.Name.LocalName);
            record[attr.Name.LocalName] = attr.Value ?? string.Empty;
        }
        foreach (var element in root.Elements())
        {
            headers.Add(element.Name.LocalName);
            record[element.Name.LocalName] = element.Value ?? string.Empty;
        }
        records.Add(record);
    }

    private async Task ProcessTextFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var headers = new List<string> { AppConstants.FileProcessing.LINE_NUMBER_COLUMN, AppConstants.FileProcessing.CONTENT_COLUMN };
            var records = ExtractTextData(lines);

            PopulateDataSet(dataSet, headers, records, fileStream.Length, context, filePath);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(context, filePath, ex, AppConstants.FileProcessing.TEXT_FILE_TYPE);
        }
    }

    private List<Dictionary<string, object>> ExtractTextData(string[] lines)
    {
        var maxRows = Math.Min(lines.Length, _dataProcessingConfig.MaxRowsPerDataset);
        var records = new List<Dictionary<string, object>>(maxRows); // Pre-allocate capacity

        for (int i = 0; i < maxRows; i++)
        {
            var record = new Dictionary<string, object>(2) // Pre-allocate for 2 fields
            {
                [AppConstants.FileProcessing.LINE_NUMBER_COLUMN] = i + 1,
                [AppConstants.FileProcessing.CONTENT_COLUMN] = lines[i]
            };
            records.Add(record);
        }

        return records;
    }

    #endregion

    #region Utility Methods

    private async Task<string> GenerateDataHashAsync(string filePath)
    {
        try
        {
            using var stream = await _storageService.GetFileAsync(filePath);
            var hash = await SHA256.HashDataAsync(stream);
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            var correlationId = GetCorrelationId();
            var context = _infrastructure.StructuredLogging.CreateContext(
                "GenerateDataHashAsync",
                correlationId,
                AppConstants.Auth.AnonymousUser,
                new Dictionary<string, object> { ["FilePath"] = filePath });

            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FAILED_GENERATE_DATA_HASH_WARNING, new Dictionary<string, object>
            {
                ["FilePath"] = filePath,
                ["ErrorMessage"] = ex.Message
            });
            return string.Empty; // Return empty string instead of throwing
        }
    }

    private static FileType GetFileTypeFromExtension(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            ".csv" => FileType.CSV,
            ".json" => FileType.JSON,
            ".xlsx" or ".xls" => FileType.Excel,
            ".xml" => FileType.XML,
            ".txt" => FileType.TXT,
            ".parquet" => FileType.Parquet,
            _ => FileType.Custom
        };
    }

    private static StorageProvider GetStorageProviderFromPath(string filePath)
    {
        return filePath switch
        {
            var path when path.StartsWith("s3://") => StorageProvider.S3,
            var path when path.StartsWith("azure://") => StorageProvider.Azure,
            var path when path.StartsWith("memory://") => StorageProvider.Memory,
            _ => StorageProvider.Local
        };
    }

    #endregion
}

// Custom exception types for better error handling
public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message) { }
    public FileValidationException(string message, Exception innerException) : base(message, innerException) { }
}

public class FileUploadException : Exception
{
    public FileUploadException(string message) : base(message) { }
    public FileUploadException(string message, Exception innerException) : base(message, innerException) { }
}

public class FileProcessingException : Exception
{
    public FileProcessingException(string message) : base(message) { }
    public FileProcessingException(string message, Exception innerException) : base(message, innerException) { }
}

public class UnsupportedFileTypeException : Exception
{
    public UnsupportedFileTypeException(string message) : base(message) { }
    public UnsupportedFileTypeException(string message, Exception innerException) : base(message, innerException) { }
}