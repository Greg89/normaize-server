using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
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
using System.Diagnostics;

namespace Normaize.Core.Services.FileUpload;

/// <summary>
/// Service for processing different file types and extracting data.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public class FileProcessingService : IFileProcessingService
{
    private readonly DataProcessingConfiguration _dataProcessingConfig;
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly IStorageService _storageService;
    private readonly IDataProcessingInfrastructure _infrastructure;
    private readonly IFileValidationService _validationService;
    private readonly IFileUtilityService _utilityService;

    public FileProcessingService(
        IOptions<DataProcessingConfiguration> dataProcessingConfig,
        IOptions<FileUploadConfiguration> fileUploadConfig,
        IStorageService storageService,
        IDataProcessingInfrastructure infrastructure,
        IFileValidationService validationService,
        IFileUtilityService utilityService)
    {
        ArgumentNullException.ThrowIfNull(dataProcessingConfig);
        ArgumentNullException.ThrowIfNull(fileUploadConfig);
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(infrastructure);
        ArgumentNullException.ThrowIfNull(validationService);
        ArgumentNullException.ThrowIfNull(utilityService);

        _dataProcessingConfig = dataProcessingConfig.Value;
        _fileUploadConfig = fileUploadConfig.Value;
        _storageService = storageService;
        _infrastructure = infrastructure;
        _validationService = validationService;
        _utilityService = utilityService;
    }

    public async Task<DataSet> ProcessFileAsync(string filePath, string fileType)
    {
        return await ExecuteProcessingOperationAsync(
            operationName: nameof(ProcessFileAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath,
                [AppConstants.DataProcessing.METADATA_FILE_TYPE] = fileType
            },
            validation: () => _validationService.ValidateFileProcessingInputs(filePath, fileType),
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
                await _validationService.ValidateFileExistsAsync(filePath, context);

                var dataSet = CreateInitialDataSet(filePath, fileType);

                await ProcessFileByTypeAsync(filePath, fileType, dataSet, context);
                await FinalizeDataSetAsync(filePath, dataSet);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_PROCESSED_SUCCESS);

                return dataSet;
            });
    }

    public async Task ProcessCsvFileAsync(string filePath, DataSet dataSet, IOperationContext context)
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
            HandleProcessingError(filePath, AppConstants.FileProcessing.CSV_EXTENSION, dataSet, context, ex);
        }
    }

    public async Task ProcessJsonFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var jsonContent = await reader.ReadToEndAsync();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            var (headers, records) = ExtractJsonData(jsonElement, context, filePath);
            var limitedHeaders = LimitColumns([.. headers], context, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, context, filePath);
        }
        catch (JsonException ex)
        {
            var error = string.Format(AppConstants.FileUpload.JSON_PARSING_ERROR, ex.Message);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.JSON_PARSING_FAILED_ERROR, new Dictionary<string, object>
            {
                [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
                [AppConstants.DataProcessing.METADATA_ERROR_MESSAGE] = ex.Message
            });
            throw new FileProcessingException(error, ex);
        }
    }

    public async Task ProcessExcelFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
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
                [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
                [AppConstants.DataProcessing.METADATA_ERROR_MESSAGE] = ex.Message
            });
            throw new FileProcessingException(error, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(context, filePath, ex, AppConstants.FileProcessing.EXCEL_FILE_TYPE);
        }
    }

    public async Task ProcessXmlFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var xmlContent = await reader.ReadToEndAsync();
            var doc = XDocument.Parse(xmlContent);

            var (headers, records) = ExtractXmlData(doc);
            var limitedHeaders = LimitColumns([.. headers], context, filePath);

            PopulateDataSet(dataSet, limitedHeaders, records, fileStream.Length, context, filePath);
        }
        catch (XmlException ex)
        {
            var error = string.Format(AppConstants.FileUpload.XML_PARSING_ERROR, ex.Message);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.XML_PARSING_FAILED_ERROR, new Dictionary<string, object>
            {
                [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
                [AppConstants.DataProcessing.METADATA_ERROR_MESSAGE] = ex.Message
            });
            throw new FileProcessingException(error, ex);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(context, filePath, ex, AppConstants.FileProcessing.XML_FILE_TYPE);
        }
    }

    public async Task ProcessTextFileAsync(string filePath, DataSet dataSet, IOperationContext context)
    {
        try
        {
            using var fileStream = await _storageService.GetFileAsync(filePath);
            using var reader = new StreamReader(fileStream);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split(AppConstants.FileProcessingConstants.NEWLINE_CHAR, StringSplitOptions.RemoveEmptyEntries);

            var headers = new List<string> { AppConstants.FileProcessing.LINE_NUMBER_COLUMN, AppConstants.FileProcessing.CONTENT_COLUMN };
            var records = ExtractTextData(lines);

            PopulateDataSet(dataSet, headers, records, fileStream.Length, context, filePath);
        }
        catch (JsonException ex)
        {
            HandleJsonSerializationError(context, filePath, ex, AppConstants.FileProcessing.TEXT_FILE_TYPE);
        }
    }

    public async Task FinalizeDataSetAsync(string filePath, DataSet dataSet)
    {
        // Generate data hash for change detection
        dataSet.DataHash = await _utilityService.GenerateDataHashAsync(filePath);

        // Determine storage strategy
        dataSet.UseSeparateTable = ShouldUseSeparateTable(dataSet);

        // Only mark as processed if there are no errors
        if (string.IsNullOrEmpty(dataSet.ProcessingErrors))
        {
            dataSet.IsProcessed = true;
            dataSet.ProcessedAt = DateTime.UtcNow;
        }
    }

    public bool ShouldUseSeparateTable(DataSet dataSet) =>
        dataSet.RowCount >= _dataProcessingConfig.MaxRowsPerDataset ||
        dataSet.FileSize > _fileUploadConfig.MaxFileSize;

    public void HandleProcessingError(string filePath, string fileType, DataSet dataSet, IOperationContext context, Exception ex)
    {
        var error = string.Format(AppConstants.FileUpload.ERROR_PROCESSING_FILE, filePath, ex.Message);
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.UNEXPECTED_ERROR_FILE_PROCESSING, new Dictionary<string, object>
        {
            [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
            [AppConstants.DataProcessing.METADATA_FILE_TYPE] = fileType,
            [AppConstants.DataProcessing.METADATA_ERROR_MESSAGE] = ex.Message
        });

        dataSet.IsProcessed = false;
        dataSet.ProcessingErrors = error;
    }

    #region Private Methods

    private async Task<T> ExecuteProcessingOperationAsync<T>(
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
                ex is FileProcessingException)
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
        if (metadata == null) return string.Format(AppConstants.FileProcessingConstants.FAILED_TO_COMPLETE_OPERATION, operationName);

        // Handle specific operation types with detailed error messages
        switch (operationName)
        {
            case nameof(ProcessFileAsync):
                var filePath = metadata.TryGetValue(AppConstants.FileProcessing.FILE_PATH_KEY, out var path) ? path?.ToString() : AppConstants.FileProcessingConstants.UNKNOWN_FILE_PATH;
                var fileType = metadata.TryGetValue(AppConstants.DataProcessing.METADATA_FILE_TYPE, out var type) ? type?.ToString() : AppConstants.FileProcessingConstants.UNKNOWN_FILE_TYPE;
                return string.Format(AppConstants.FileProcessingConstants.FAILED_TO_COMPLETE_FILE_PROCESSING, operationName, filePath, fileType);

            default:
                return string.Format(AppConstants.FileProcessingConstants.FAILED_TO_COMPLETE_OPERATION, operationName);
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    private static DataSet CreateInitialDataSet(string filePath, string fileType) => new()
    {
        FileName = Path.GetFileName(filePath),
        FilePath = filePath,
        FileType = GetFileTypeFromExtension(fileType),
        FileSize = AppConstants.FileProcessing.DEFAULT_FILE_SIZE, // Will be calculated during processing
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
        AppConstants.FileProcessing.CSV_EXTENSION => ProcessCsvFileAsync,
        AppConstants.FileProcessing.JSON_EXTENSION => ProcessJsonFileAsync,
        AppConstants.FileProcessing.XLSX_EXTENSION or AppConstants.FileProcessing.XLS_EXTENSION => ProcessExcelFileAsync,
        AppConstants.FileProcessing.XML_EXTENSION => ProcessXmlFileAsync,
        AppConstants.FileProcessing.TXT_EXTENSION => ProcessTextFileAsync,
        _ => throw new UnsupportedFileTypeException(string.Format(AppConstants.FileUpload.UNSUPPORTED_FILE_TYPE_ERROR, fileType))
    };

    #endregion

    #region CSV Processing Methods

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

            if (headers.Count == AppConstants.FileProcessing.DEFAULT_ROW_COUNT)
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.CSV_NO_HEADERS_WARNING, new Dictionary<string, object>
                {
                    [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath
                });
            }
        }

        var rowCount = AppConstants.FileProcessing.DEFAULT_ROW_COUNT;
        var maxRows = _dataProcessingConfig.MaxRowsPerDataset;

        // Pre-allocate capacity for better performance
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY);

        while (await csv.ReadAsync() && rowCount < maxRows)
        {
            var record = new Dictionary<string, object>(headers.Count);
            foreach (var header in headers)
            {
                var field = csv.GetField(header);
                record[header] = field ?? AppConstants.FileProcessingConstants.EMPTY_STRING;
            }
            records.Add(record);
            rowCount++;
        }

        return (headers, records);
    }

    private void HandleCsvProcessingError(IOperationContext context, string filePath, CsvHelperException ex)
    {
        var error = string.Format(AppConstants.FileUpload.CSV_PARSING_ERROR, ex.Message);
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.CSV_PARSING_FAILED_ERROR, new Dictionary<string, object>
        {
            [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
            [AppConstants.DataProcessing.METADATA_ERROR_MESSAGE] = ex.Message
        });
        throw new FileProcessingException(error, ex);
    }

    #endregion

    #region JSON Processing Methods

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
                    [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
                    [AppConstants.DataProcessing.METADATA_VALUE_KIND] = jsonElement.ValueKind
                });
                throw new FileProcessingException(error);
        }

        return (headers, records);
    }

    private void ExtractJsonArrayData(JsonElement jsonElement, HashSet<string> headers, List<Dictionary<string, object>> records)
    {
        var maxRows = _dataProcessingConfig.MaxRowsPerDataset;
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY);

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

    #endregion

    #region Excel Processing Methods

    private static ExcelWorksheet GetWorksheet(ExcelPackage package)
    {
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet != null) return worksheet;

        var error = AppConstants.FileProcessingConstants.EXCEL_WORKSHEET_NOT_FOUND;
        throw new InvalidOperationException(error);
    }

    private static List<string> ExtractExcelHeaders(ExcelWorksheet worksheet)
    {
        var headers = new List<string>();
        var headerRow = worksheet.Cells[AppConstants.FileProcessingConstants.EXCEL_HEADER_ROW, AppConstants.FileProcessingConstants.EXCEL_DEFAULT_COLUMN, AppConstants.FileProcessingConstants.EXCEL_HEADER_ROW, worksheet.Dimension?.Columns ?? AppConstants.FileProcessingConstants.EXCEL_DEFAULT_COLUMN];

        foreach (var cell in headerRow)
        {
            headers.Add(cell.Value?.ToString() ?? $"{AppConstants.FileProcessing.DEFAULT_COLUMN_PREFIX}{cell.Start.Column}");
        }

        return headers;
    }

    private List<Dictionary<string, object>> ExtractExcelData(ExcelWorksheet worksheet, List<string> headers)
    {
        var records = new List<Dictionary<string, object>>();
        var rowCount = AppConstants.FileProcessing.DEFAULT_ROW_COUNT;
        var maxRows = _dataProcessingConfig.MaxRowsPerDataset;
        var maxCols = headers.Count;

        // Pre-allocate capacity for better performance
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY);

        // Optimize by reading entire range at once when possible
        var dataRange = worksheet.Cells[AppConstants.FileProcessingConstants.EXCEL_DATA_START_ROW, AppConstants.FileProcessingConstants.EXCEL_DEFAULT_COLUMN,
            Math.Min(worksheet.Dimension?.Rows ?? AppConstants.FileProcessingConstants.EXCEL_DEFAULT_COLUMN, AppConstants.FileProcessingConstants.EXCEL_DATA_START_ROW + maxRows - 1),
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
                    record[headers[col]] = cellValue?.ToString() ?? AppConstants.FileProcessingConstants.EMPTY_STRING;
                }
                records.Add(record);
                rowCount++;
            }
        }
        else
        {
            // Fallback to cell-by-cell access if range reading fails
            for (int row = AppConstants.FileProcessingConstants.EXCEL_DATA_START_ROW; row <= (worksheet.Dimension?.Rows ?? AppConstants.FileProcessingConstants.EXCEL_DEFAULT_COLUMN) && rowCount < maxRows; row++)
            {
                var record = new Dictionary<string, object>(maxCols);
                for (int col = AppConstants.FileProcessingConstants.EXCEL_DEFAULT_COLUMN; col <= maxCols; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    record[headers[col - AppConstants.FileProcessingConstants.EXCEL_DEFAULT_COLUMN]] = cellValue?.ToString() ?? AppConstants.FileProcessingConstants.EMPTY_STRING;
                }
                records.Add(record);
                rowCount++;
            }
        }

        return records;
    }

    #endregion

    #region XML Processing Methods

    private (HashSet<string> Headers, List<Dictionary<string, object>> Records) ExtractXmlData(XDocument doc)
    {
        var records = new List<Dictionary<string, object>>();
        var headers = new HashSet<string>();

        var root = doc.Root;
        if (root == null) return (headers, records);

        var children = root.Elements().ToList();
        if (children.Count > AppConstants.FileProcessingConstants.EXCEL_DEFAULT_CHILDREN_COUNT)
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
        records.Capacity = Math.Min(maxRows, AppConstants.FileProcessing.DEFAULT_RECORDS_CAPACITY);

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
                record[element.Name.LocalName] = element.Value ?? AppConstants.FileProcessingConstants.EMPTY_STRING;
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
            record[attr.Name.LocalName] = attr.Value ?? AppConstants.FileProcessingConstants.EMPTY_STRING;
        }
        foreach (var element in root.Elements())
        {
            headers.Add(element.Name.LocalName);
            record[element.Name.LocalName] = element.Value ?? AppConstants.FileProcessingConstants.EMPTY_STRING;
        }
        records.Add(record);
    }

    #endregion

    #region Text Processing Methods

    private List<Dictionary<string, object>> ExtractTextData(string[] lines)
    {
        var maxRows = Math.Min(lines.Length, _dataProcessingConfig.MaxRowsPerDataset);
        var records = new List<Dictionary<string, object>>(maxRows);

        for (int i = 0; i < maxRows; i++)
        {
            var record = new Dictionary<string, object>(AppConstants.FileProcessing.DEFAULT_DICTIONARY_CAPACITY)
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

    private List<string> LimitColumns(List<string> headers, IOperationContext context, string filePath)
    {
        if (headers.Count <= _dataProcessingConfig.MaxColumnsPerDataset)
            return headers;

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_TOO_MANY_COLUMNS_WARNING, new Dictionary<string, object>
        {
            [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
            [AppConstants.DataProcessing.METADATA_COLUMN_COUNT] = headers.Count,
            [AppConstants.DataProcessing.METADATA_MAX_COLUMNS] = _dataProcessingConfig.MaxColumnsPerDataset
        });
        return [.. headers.Take(_dataProcessingConfig.MaxColumnsPerDataset)];
    }

    private void PopulateDataSet(DataSet dataSet, List<string> headers, List<Dictionary<string, object>> records,
        long fileSize, IOperationContext context, string filePath)
    {
        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = records.Count;
        dataSet.FileSize = fileSize;

        // Use global JSON configuration for consistent camelCase output
        dataSet.Schema = JsonConfiguration.Serialize(headers);

        // Only serialize preview data if needed
        if (records.Count > AppConstants.FileProcessing.DEFAULT_ROW_COUNT)
        {
            var previewRecords = records.Take(_dataProcessingConfig.MaxPreviewRows).ToList();
            
            // Create standardized PreviewData format
            var previewData = new DataSetPreviewDto
            {
                Columns = headers,
                Rows = previewRecords,
                TotalRows = records.Count,
                MaxPreviewRows = _dataProcessingConfig.MaxPreviewRows,
                PreviewRowCount = previewRecords.Count
            };
            
            dataSet.PreviewData = JsonConfiguration.Serialize(previewData);
        }

        // Only serialize full data if within limits
        if (records.Count < _dataProcessingConfig.MaxRowsPerDataset)
        {
            dataSet.ProcessedData = JsonConfiguration.Serialize(records);
        }

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_PROCESSING_COMPLETED_DEBUG, new Dictionary<string, object>
        {
            [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
            [AppConstants.DataProcessing.METADATA_ROW_COUNT] = records.Count,
            [AppConstants.DataProcessing.METADATA_COLUMN_COUNT] = headers.Count
        });
    }

    private void HandleJsonSerializationError(IOperationContext context, string filePath, JsonException ex, string fileType)
    {
        var error = string.Format(AppConstants.FileUpload.JSON_SERIALIZATION_ERROR, fileType, ex.Message);
        _infrastructure.StructuredLogging.LogStep(context, string.Format(AppConstants.FileUpload.JSON_SERIALIZATION_FAILED_ERROR, fileType), new Dictionary<string, object>
        {
            [AppConstants.DataProcessing.METADATA_FILE_PATH] = filePath,
            [AppConstants.DataProcessing.METADATA_FILE_TYPE] = fileType,
            [AppConstants.DataProcessing.METADATA_ERROR_MESSAGE] = ex.Message
        });
        throw new FileProcessingException(error, ex);
    }

    private static FileType GetFileTypeFromExtension(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            AppConstants.FileProcessing.CSV_EXTENSION => FileType.CSV,
            AppConstants.FileProcessing.JSON_EXTENSION => FileType.JSON,
            AppConstants.FileProcessing.XLSX_EXTENSION or AppConstants.FileProcessing.XLS_EXTENSION => FileType.Excel,
            AppConstants.FileProcessing.XML_EXTENSION => FileType.XML,
            AppConstants.FileProcessing.TXT_EXTENSION => FileType.TXT,
            AppConstants.FileProcessing.PARQUET_EXTENSION => FileType.Parquet,
            _ => FileType.Custom
        };
    }

    private static StorageProvider GetStorageProviderFromPath(string filePath)
    {
        return filePath switch
        {
            var path when path.StartsWith(AppConstants.FileProcessing.S3_PREFIX) => StorageProvider.S3,
            var path when path.StartsWith(AppConstants.FileProcessing.AZURE_PREFIX) => StorageProvider.Azure,
            var path when path.StartsWith(AppConstants.FileProcessing.MEMORY_PREFIX) => StorageProvider.Memory,
            _ => StorageProvider.Local
        };
    }

    #endregion
}

// Custom exception types for file processing errors
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