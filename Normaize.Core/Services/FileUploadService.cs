using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
using System.Text;

namespace Normaize.Core.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IStorageService _storageService;
    private readonly int _maxRowsForInlineStorage;
    private readonly int _maxFileSizeForInlineStorage;

    public FileUploadService(
        IConfiguration configuration, 
        ILogger<FileUploadService> logger,
        IStorageService storageService)
    {
        _configuration = configuration;
        _logger = logger;
        _storageService = storageService;
        _maxRowsForInlineStorage = configuration.GetValue<int>("DataProcessing:MaxRowsPerDataset", 10000);
        _maxFileSizeForInlineStorage = configuration.GetValue<int>("FileUpload:MaxFileSize", 10485760); // 10MB
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        return await _storageService.SaveFileAsync(fileRequest);
    }

    public Task<bool> ValidateFileAsync(FileUploadRequest fileRequest)
    {
        if (fileRequest.FileStream == null || fileRequest.FileStream.Length == 0)
            return Task.FromResult(false);

        var maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSize", 100 * 1024 * 1024); // 100MB default
        if (fileRequest.FileSize > maxFileSize)
            return Task.FromResult(false);

        var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? 
                               new[] { ".csv", ".json", ".xlsx", ".xls", ".xml", ".parquet", ".txt" };

        var fileExtension = Path.GetExtension(fileRequest.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<DataSet> ProcessFileAsync(string filePath, string fileType)
    {
        // Get file size from storage service
        var fileExists = await _storageService.FileExistsAsync(filePath);
        if (!fileExists)
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var dataSet = new DataSet
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileType = fileType,
            FileSize = 0, // Will be calculated during processing
            UploadedAt = DateTime.UtcNow,
            StorageProvider = filePath.StartsWith("sftp://") ? "SFTP" : 
                             filePath.StartsWith("minio://") ? "MinIO" :
                             filePath.StartsWith("s3://") ? "S3" : "Local"
        };

        try
        {
            switch (fileType.ToLowerInvariant())
            {
                case ".csv":
                    await ProcessCsvFileAsync(filePath, dataSet);
                    break;
                case ".json":
                    await ProcessJsonFileAsync(filePath, dataSet);
                    break;
                case ".xlsx":
                case ".xls":
                    await ProcessExcelFileAsync(filePath, dataSet);
                    break;
                case ".xml":
                    await ProcessXmlFileAsync(filePath, dataSet);
                    break;
                case ".txt":
                    await ProcessTextFileAsync(filePath, dataSet);
                    break;
                default:
                    throw new NotSupportedException($"File type {fileType} is not supported");
            }

            // Generate data hash for change detection
            dataSet.DataHash = await GenerateDataHashAsync(filePath);
            
            // Determine storage strategy
            dataSet.UseSeparateTable = dataSet.RowCount > _maxRowsForInlineStorage || 
                                      dataSet.FileSize > _maxFileSizeForInlineStorage;

            dataSet.IsProcessed = true;
            dataSet.ProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FilePath}", filePath);
            dataSet.IsProcessed = false;
            dataSet.ProcessingErrors = ex.Message;
        }

        return dataSet;
    }

    private async Task ProcessCsvFileAsync(string filePath, DataSet dataSet)
    {
        using var fileStream = await _storageService.GetFileAsync(filePath);
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            Delimiter = ",",
            HasHeaderRecord = true
        });

        var records = new List<Dictionary<string, object>>();
        var headers = new List<string>();

        if (await csv.ReadAsync())
        {
            csv.ReadHeader();
            headers = csv.HeaderRecord?.ToList() ?? new List<string>();
        }

        var rowCount = 0;
        var maxRows = 10000; // Limit for processing
        
        while (await csv.ReadAsync() && rowCount < maxRows)
        {
            var record = new Dictionary<string, object>();
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header) ?? "";
            }
            records.Add(record);
            rowCount++;
        }

        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = rowCount;
        dataSet.Schema = JsonSerializer.Serialize(headers);
        
        // Store preview data (first 10 rows)
        dataSet.PreviewData = JsonSerializer.Serialize(records.Take(10).ToList());
        
        // Store full data if small enough
        if (rowCount <= _maxRowsForInlineStorage)
        {
            dataSet.ProcessedData = JsonSerializer.Serialize(records);
        }
    }

    private async Task ProcessJsonFileAsync(string filePath, DataSet dataSet)
    {
        using var fileStream = await _storageService.GetFileAsync(filePath);
        using var reader = new StreamReader(fileStream);
        var jsonContent = await reader.ReadToEndAsync();
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);

        var records = new List<Dictionary<string, object>>();
        var headers = new HashSet<string>();

        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray().Take(10000))
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
        else if (jsonElement.ValueKind == JsonValueKind.Object)
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

        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = records.Count;
        dataSet.Schema = JsonSerializer.Serialize(headers.ToList());
        dataSet.PreviewData = JsonSerializer.Serialize(records.Take(10).ToList());
        
        if (records.Count <= _maxRowsForInlineStorage)
        {
            dataSet.ProcessedData = JsonSerializer.Serialize(records);
        }
    }

    private async Task ProcessExcelFileAsync(string filePath, DataSet dataSet)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var fileStream = await _storageService.GetFileAsync(filePath);
        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? 
                       throw new InvalidOperationException("No worksheet found in Excel file");

        var headers = new List<string>();
        var records = new List<Dictionary<string, object>>();

        // Read headers
        var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension?.Columns ?? 1];
        foreach (var cell in headerRow)
        {
            headers.Add(cell.Value?.ToString() ?? $"Column{cell.Start.Column}");
        }

        // Read data
        var rowCount = 0;
        var maxRows = 10000;
        for (int row = 2; row <= (worksheet.Dimension?.Rows ?? 1) && rowCount < maxRows; row++)
        {
            var record = new Dictionary<string, object>();
            for (int col = 1; col <= headers.Count; col++)
            {
                var cellValue = worksheet.Cells[row, col].Value;
                record[headers[col - 1]] = cellValue?.ToString() ?? "";
            }
            records.Add(record);
            rowCount++;
        }

        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = rowCount;
        dataSet.Schema = JsonSerializer.Serialize(headers);
        dataSet.PreviewData = JsonSerializer.Serialize(records.Take(10).ToList());
        
        if (rowCount <= _maxRowsForInlineStorage)
        {
            dataSet.ProcessedData = JsonSerializer.Serialize(records);
        }
    }

    private async Task ProcessXmlFileAsync(string filePath, DataSet dataSet)
    {
        using var fileStream = await _storageService.GetFileAsync(filePath);
        using var reader = new StreamReader(fileStream);
        var xmlContent = await reader.ReadToEndAsync();
        var doc = XDocument.Parse(xmlContent);
        
        var records = new List<Dictionary<string, object>>();
        var headers = new HashSet<string>();

        // Try to find repeating elements (common pattern in XML data)
        var root = doc.Root;
        if (root != null)
        {
            var children = root.Elements().ToList();
            if (children.Count > 0)
            {
                // Assume first child is the template for data rows
                var firstChild = children.First();
                var childElements = firstChild.Elements().ToList();
                
                // Extract headers from first element
                foreach (var element in childElements)
                {
                    headers.Add(element.Name.LocalName);
                }
                
                // Process all child elements as data rows
                foreach (var child in children.Take(10000))
                {
                    var record = new Dictionary<string, object>();
                    foreach (var element in child.Elements())
                    {
                        record[element.Name.LocalName] = element.Value ?? "";
                    }
                    records.Add(record);
                }
            }
            else
            {
                // Single object - convert attributes and elements to record
                var record = new Dictionary<string, object>();
                foreach (var attr in root.Attributes())
                {
                    headers.Add(attr.Name.LocalName);
                    record[attr.Name.LocalName] = attr.Value ?? "";
                }
                foreach (var element in root.Elements())
                {
                    headers.Add(element.Name.LocalName);
                    record[element.Name.LocalName] = element.Value ?? "";
                }
                records.Add(record);
            }
        }

        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = records.Count;
        dataSet.Schema = JsonSerializer.Serialize(headers.ToList());
        dataSet.PreviewData = JsonSerializer.Serialize(records.Take(10).ToList());
        
        if (records.Count <= _maxRowsForInlineStorage)
        {
            dataSet.ProcessedData = JsonSerializer.Serialize(records);
        }
    }

    private async Task ProcessTextFileAsync(string filePath, DataSet dataSet)
    {
        using var fileStream = await _storageService.GetFileAsync(filePath);
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var records = new List<Dictionary<string, object>>();
        var headers = new List<string> { "LineNumber", "Content" };

        for (int i = 0; i < Math.Min(lines.Length, 10000); i++)
        {
            var record = new Dictionary<string, object>
            {
                ["LineNumber"] = i + 1,
                ["Content"] = lines[i]
            };
            records.Add(record);
        }

        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = records.Count;
        dataSet.Schema = JsonSerializer.Serialize(headers);
        dataSet.PreviewData = JsonSerializer.Serialize(records.Take(10).ToList());
        
        if (records.Count <= _maxRowsForInlineStorage)
        {
            dataSet.ProcessedData = JsonSerializer.Serialize(records);
        }
    }

    private async Task<string> GenerateDataHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = await _storageService.GetFileAsync(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }

    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            await _storageService.DeleteFileAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
        }
    }
} 