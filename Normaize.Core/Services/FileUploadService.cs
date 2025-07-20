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

namespace Normaize.Core.Services;

public class FileUploadService : IFileUploadService
{
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly DataProcessingConfiguration _dataProcessingConfig;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IStorageService _storageService;
    private readonly IStructuredLoggingService _structuredLoggingService;
    private readonly IChaosEngineeringService _chaosEngineeringService;

    // Constants moved to AppConstants.FileProcessing

    public FileUploadService(
        IOptions<FileUploadConfiguration> fileUploadConfig,
        IOptions<DataProcessingConfiguration> dataProcessingConfig,
        ILogger<FileUploadService> logger,
        IStorageService storageService,
        IStructuredLoggingService structuredLoggingService,
        IChaosEngineeringService chaosEngineeringService)
    {
        _fileUploadConfig = fileUploadConfig?.Value ?? throw new ArgumentNullException(nameof(fileUploadConfig));
        _dataProcessingConfig = dataProcessingConfig?.Value ?? throw new ArgumentNullException(nameof(dataProcessingConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _structuredLoggingService = structuredLoggingService ?? throw new ArgumentNullException(nameof(structuredLoggingService));
        _chaosEngineeringService = chaosEngineeringService ?? throw new ArgumentNullException(nameof(chaosEngineeringService));
        
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
        _logger.LogInformation("FileUploadService initialized with configuration: " +
            "MaxFileSize={MaxFileSize}MB, MaxRowsPerDataset={MaxRowsPerDataset}, " +
            "AllowedExtensions=[{AllowedExtensions}], BlockedExtensions=[{BlockedExtensions}]",
            _fileUploadConfig.MaxFileSize / AppConstants.FileProcessing.BYTES_PER_MEGABYTE,
            _dataProcessingConfig.MaxRowsPerDataset,
            string.Join(", ", _fileUploadConfig.AllowedExtensions),
            string.Join(", ", _fileUploadConfig.BlockedExtensions));
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        var correlationId = GenerateCorrelationId();
        
        try
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_UPLOAD_STARTED);

            // Apply chaos engineering for file upload
            await _chaosEngineeringService.ExecuteChaosAsync(AppConstants.FileProcessing.STORAGE_FAILURE_SCENARIO, correlationId, "SaveFileAsync", 
                async () => await Task.Delay(100), new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest.FileName });

            // Validate file before saving
            var isValid = await ValidateFileAsync(fileRequest);
            if (!isValid)
            {
                _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_VALIDATION_FAILED);
                throw new FileValidationException($"File validation failed for {fileRequest.FileName}");
            }

            var filePath = await _storageService.SaveFileAsync(fileRequest);
            
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_UPLOAD_SUCCESS);
            
            return filePath;
        }
        catch (FileValidationException)
        {
            // Re-throw validation exceptions as they are already logged
            throw;
        }
        catch (Exception ex)
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_UPLOAD_FAILED);
            throw new FileUploadException($"Failed to save file {fileRequest.FileName}", ex);
        }
    }

    public Task<bool> ValidateFileAsync(FileUploadRequest fileRequest)
    {
        var correlationId = GenerateCorrelationId();
        
        try
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_VALIDATION_STARTED);

            if (!IsFileSizeValid(fileRequest.FileSize, correlationId, fileRequest.FileName))
            {
                _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_SIZE_VALIDATION_FAILED);
                return Task.FromResult(false);
            }

            var fileExtension = GetFileExtension(fileRequest.FileName);
            
            if (!IsFileExtensionValid(fileExtension, correlationId, fileRequest.FileName))
            {
                _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_EXTENSION_VALIDATION_FAILED);
                return Task.FromResult(false);
            }

            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_VALIDATION_PASSED);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_VALIDATION_ERROR);
            return Task.FromResult(false);
        }
    }

    public async Task<DataSet> ProcessFileAsync(string filePath, string fileType)
    {
        var correlationId = GenerateCorrelationId();
        DataSet dataSet = null!;
        
        try
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_PROCESSING_STARTED);

            // Apply chaos engineering for file processing
            await _chaosEngineeringService.ExecuteChaosAsync(AppConstants.FileProcessing.PROCESSING_DELAY_SCENARIO, correlationId, "ProcessFileAsync", 
                async () => await Task.Delay(100), new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_TYPE_KEY] = fileType });

            // Validate file exists
            await ValidateFileExistsAsync(filePath, correlationId);

            dataSet = CreateInitialDataSet(filePath, fileType);

            await ProcessFileByTypeAsync(filePath, fileType, dataSet, correlationId);
            await FinalizeDataSetAsync(filePath, dataSet);

            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_PROCESSED_SUCCESS);
            
            return dataSet;
        }
        catch (UnsupportedFileTypeException)
        {
            // Re-throw specific exceptions
            throw;
        }
        catch (FileProcessingException)
        {
            // Re-throw processing exceptions
            throw;
        }
        catch (Exception ex)
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_PROCESSING_FAILED);
            if (dataSet != null)
            {
                HandleProcessingError(filePath, fileType, dataSet, correlationId, ex);
            }
            throw;
        }
    }

    // Helper methods for better code organization
    private static string GenerateCorrelationId() => Guid.NewGuid().ToString();

    private bool IsFileSizeValid(long fileSize, string correlationId, string fileName)
    {
        if (fileSize <= _fileUploadConfig.MaxFileSize) return true;
        
        _logger.LogWarning("File size exceeds limit. CorrelationId: {CorrelationId}, FileName: {FileName}, FileSize: {FileSize}, MaxSize: {MaxSize}", 
            correlationId, fileName, fileSize, _fileUploadConfig.MaxFileSize);
        return false;
    }

    private static string GetFileExtension(string fileName) => 
        Path.GetExtension(fileName).ToLowerInvariant();

    private bool IsFileExtensionValid(string fileExtension, string correlationId, string fileName)
    {
        // Check if extension is blocked
        if (_fileUploadConfig.BlockedExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("File extension is blocked. CorrelationId: {CorrelationId}, FileName: {FileName}, Extension: {Extension}", 
                correlationId, fileName, fileExtension);
            return false;
        }
        
        // Check if extension is allowed
        if (!_fileUploadConfig.AllowedExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("File extension not allowed. CorrelationId: {CorrelationId}, FileName: {FileName}, Extension: {Extension}, AllowedExtensions: {AllowedExtensions}", 
                correlationId, fileName, fileExtension, string.Join(", ", _fileUploadConfig.AllowedExtensions));
            return false;
        }

        return true;
    }

    private async Task ValidateFileExistsAsync(string filePath, string correlationId)
    {
        var fileExists = await _storageService.FileExistsAsync(filePath);
        if (!fileExists)
        {
            var error = $"File not found: {filePath}";
            _logger.LogError("File not found during processing. CorrelationId: {CorrelationId}, FilePath: {FilePath}", 
                correlationId, filePath);
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

    private async Task ProcessFileByTypeAsync(string filePath, string fileType, DataSet dataSet, string correlationId)
    {
        var processor = GetFileProcessor(fileType);
        await processor(filePath, dataSet, correlationId);
    }

    private delegate Task FileProcessor(string filePath, DataSet dataSet, string correlationId);

    private FileProcessor GetFileProcessor(string fileType) => fileType.ToLowerInvariant() switch
    {
        ".csv" => ProcessCsvFileAsync,
        ".json" => ProcessJsonFileAsync,
        ".xlsx" or ".xls" => ProcessExcelFileAsync,
        ".xml" => ProcessXmlFileAsync,
        ".txt" => ProcessTextFileAsync,
        _ => throw new UnsupportedFileTypeException($"File type {fileType} is not supported")
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

    private void HandleProcessingError(string filePath, string fileType, DataSet dataSet, string correlationId, Exception ex)
    {
        var error = $"Error processing file {filePath}: {ex.Message}";
        _logger.LogError(ex, "Unexpected error during file processing. CorrelationId: {CorrelationId}, FilePath: {FilePath}, FileType: {FileType}", 
            correlationId, filePath, fileType);
        
        dataSet.IsProcessed = false;
        dataSet.ProcessingErrors = error;
    }

    private async Task ProcessCsvFileAsync(string filePath, DataSet dataSet, string correlationId)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            using var csv = CreateCsvReader(reader);

            var (headers, records) = await ExtractCsvDataAsync(csv, correlationId, filePath);
            var limitedHeaders = LimitColumns(headers, correlationId, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, correlationId, filePath);
        }
        catch (CsvHelperException ex)
        {
            HandleCsvProcessingError(correlationId, filePath, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(correlationId, filePath, ex, AppConstants.FileProcessing.CSV_FILE_TYPE);
        }
        catch (Exception ex)
        {
            HandleProcessingError(filePath, ".csv", dataSet, correlationId, ex);
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
        CsvReader csv, string correlationId, string filePath)
    {
        var records = new List<Dictionary<string, object>>();
        var headers = new List<string>();

        if (await csv.ReadAsync())
        {
            csv.ReadHeader();
            headers = csv.HeaderRecord?.ToList() ?? [];
            
            if (headers.Count == 0)
            {
                _logger.LogWarning("CSV file has no headers. CorrelationId: {CorrelationId}, FilePath: {FilePath}", 
                    correlationId, filePath);
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

    private List<string> LimitColumns(List<string> headers, string correlationId, string filePath)
    {
        if (headers.Count <= _dataProcessingConfig.MaxColumnsPerDataset) 
            return headers;

        _logger.LogWarning("File has too many columns. CorrelationId: {CorrelationId}, FilePath: {FilePath}, ColumnCount: {ColumnCount}, MaxColumns: {MaxColumns}", 
            correlationId, filePath, headers.Count, _dataProcessingConfig.MaxColumnsPerDataset);
        return headers.Take(_dataProcessingConfig.MaxColumnsPerDataset).ToList();
    }

    private void PopulateDataSet(DataSet dataSet, List<string> headers, List<Dictionary<string, object>> records, 
        long fileSize, string correlationId, string filePath)
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

        _logger.LogDebug("File processing completed. CorrelationId: {CorrelationId}, FilePath: {FilePath}, Rows: {RowCount}, Columns: {ColumnCount}", 
            correlationId, filePath, records.Count, headers.Count);
    }

    private void HandleCsvProcessingError(string correlationId, string filePath, CsvHelperException ex)
    {
        // Use string interpolation for better performance
        var error = $"CSV parsing error: {ex.Message}";
        _logger.LogError(ex, "CSV parsing failed. CorrelationId: {CorrelationId}, FilePath: {FilePath}", 
            correlationId, filePath);
        throw new FileProcessingException(error, ex);
    }

    private void HandleJsonSerializationError(string correlationId, string filePath, JsonException ex, string fileType)
    {
        // Use string interpolation for better performance
        var error = $"JSON serialization error during {fileType} processing: {ex.Message}";
        _logger.LogError(ex, "JSON serialization failed during {FileType} processing. CorrelationId: {CorrelationId}, FilePath: {FilePath}", 
            fileType, correlationId, filePath);
        throw new FileProcessingException(error, ex);
    }

    private async Task ProcessJsonFileAsync(string filePath, DataSet dataSet, string correlationId)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var jsonContent = await reader.ReadToEndAsync();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            var (headers, records) = ExtractJsonData(jsonElement, correlationId, filePath);
            var limitedHeaders = LimitColumns(headers.ToList(), correlationId, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, correlationId, filePath);
        }
        catch (JsonException ex)
        {
            var error = $"JSON parsing error: {ex.Message}";
            _logger.LogError(ex, "JSON parsing failed. CorrelationId: {CorrelationId}, FilePath: {FilePath}", 
                correlationId, filePath);
            throw new FileProcessingException(error, ex);
        }
    }

    private (HashSet<string> Headers, List<Dictionary<string, object>> Records) ExtractJsonData(
        JsonElement jsonElement, string correlationId, string filePath)
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
                var error = $"Unsupported JSON structure: {jsonElement.ValueKind}";
                _logger.LogWarning("Unsupported JSON structure. CorrelationId: {CorrelationId}, FilePath: {FilePath}, ValueKind: {ValueKind}", 
                    correlationId, filePath, jsonElement.ValueKind);
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

    private async Task ProcessExcelFileAsync(string filePath, DataSet dataSet, string correlationId)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var package = new ExcelPackage(fileStream);
            var worksheet = GetWorksheet(package);

            var headers = ExtractExcelHeaders(worksheet);
            var limitedHeaders = LimitColumns(headers, correlationId, filePath);
            var records = ExtractExcelData(worksheet, limitedHeaders);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, correlationId, filePath);
        }
        catch (InvalidOperationException ex)
        {
            var error = $"Excel processing error: {ex.Message}";
            _logger.LogError(ex, "Excel processing failed. CorrelationId: {CorrelationId}, FilePath: {FilePath}", 
                correlationId, filePath);
            throw new FileProcessingException(error, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(correlationId, filePath, ex, AppConstants.FileProcessing.EXCEL_FILE_TYPE);
        }
    }

    private static ExcelWorksheet GetWorksheet(ExcelPackage package)
    {
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet != null) return worksheet;

        var error = "No worksheet found in Excel file";
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

    private async Task ProcessXmlFileAsync(string filePath, DataSet dataSet, string correlationId)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var xmlContent = await reader.ReadToEndAsync();
            var doc = XDocument.Parse(xmlContent);
            
            var (headers, records) = ExtractXmlData(doc);
            var limitedHeaders = LimitColumns(headers.ToList(), correlationId, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, correlationId, filePath);
        }
        catch (XmlException ex)
        {
            var error = $"XML parsing error: {ex.Message}";
            _logger.LogError(ex, "XML parsing failed. CorrelationId: {CorrelationId}, FilePath: {FilePath}", 
                correlationId, filePath);
            throw new FileProcessingException(error, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(correlationId, filePath, ex, AppConstants.FileProcessing.XML_FILE_TYPE);
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

    private void ExtractXmlObjectData(XElement root, HashSet<string> headers, List<Dictionary<string, object>> records)
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

    private async Task ProcessTextFileAsync(string filePath, DataSet dataSet, string correlationId)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            var headers = new List<string> { AppConstants.FileProcessing.LINE_NUMBER_COLUMN, AppConstants.FileProcessing.CONTENT_COLUMN };
            var records = ExtractTextData(lines);

            PopulateDataSet(dataSet, headers, records, fileStream.Length, correlationId, filePath);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(correlationId, filePath, ex, AppConstants.FileProcessing.TEXT_FILE_TYPE);
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
            _logger.LogWarning(ex, "Failed to generate data hash for file {FilePath}", filePath);
            return string.Empty; // Return empty string instead of throwing
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        var correlationId = GenerateCorrelationId();
        
        try
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_DELETION_STARTED);

            // Apply chaos engineering for file deletion
            await _chaosEngineeringService.ExecuteChaosAsync(AppConstants.FileProcessing.STORAGE_FAILURE_SCENARIO, correlationId, "DeleteFileAsync", 
                async () => await Task.Delay(100), new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath });

            await _storageService.DeleteFileAsync(filePath);
            
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_DELETED_SUCCESS);
        }
        catch (Exception ex)
        {
            _structuredLoggingService.LogUserAction(AppConstants.FileUploadMessages.FILE_DELETION_FAILED);
            // Don't re-throw - log and continue
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