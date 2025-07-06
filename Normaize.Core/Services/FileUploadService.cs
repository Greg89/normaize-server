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

namespace Normaize.Core.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileUploadService> _logger;
    private readonly string _uploadPath;

    public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await fileRequest.FileStream.CopyToAsync(fileStream);
        }

        return filePath;
    }

    public Task<bool> ValidateFileAsync(FileUploadRequest fileRequest)
    {
        if (fileRequest.FileStream == null || fileRequest.FileStream.Length == 0)
            return Task.FromResult(false);

        var maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSize", 10 * 1024 * 1024); // 10MB default
        if (fileRequest.FileSize > maxFileSize)
            return Task.FromResult(false);

        var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? 
                               new[] { ".csv", ".json", ".xlsx", ".xls" };

        var fileExtension = Path.GetExtension(fileRequest.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<DataSet> ProcessFileAsync(string filePath, string fileType)
    {
        var dataSet = new DataSet
        {
            FileName = Path.GetFileName(filePath),
            FileType = fileType,
            FileSize = new FileInfo(filePath).Length,
            UploadedAt = DateTime.UtcNow
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
                default:
                    throw new NotSupportedException($"File type {fileType} is not supported");
            }

            dataSet.IsProcessed = true;
            dataSet.ProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FilePath}", filePath);
            dataSet.IsProcessed = false;
        }

        return dataSet;
    }

    private async Task ProcessCsvFileAsync(string filePath, DataSet dataSet)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var records = new List<Dictionary<string, object>>();
        var headers = new List<string>();

        if (await csv.ReadAsync())
        {
            csv.ReadHeader();
            headers = csv.HeaderRecord?.ToList() ?? new List<string>();
        }

        var rowCount = 0;
        while (await csv.ReadAsync() && rowCount < 1000) // Limit preview to 1000 rows
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
        dataSet.PreviewData = JsonSerializer.Serialize(records.Take(10).ToList());
    }

    private async Task ProcessJsonFileAsync(string filePath, DataSet dataSet)
    {
        var jsonContent = await File.ReadAllTextAsync(filePath);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent);

        var records = new List<Dictionary<string, object>>();
        var headers = new HashSet<string>();

        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray().Take(1000))
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

        dataSet.ColumnCount = headers.Count;
        dataSet.RowCount = records.Count;
        dataSet.Schema = JsonSerializer.Serialize(headers.ToList());
        dataSet.PreviewData = JsonSerializer.Serialize(records.Take(10).ToList());
    }

    private Task ProcessExcelFileAsync(string filePath, DataSet dataSet)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(new FileInfo(filePath));
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
        for (int row = 2; row <= (worksheet.Dimension?.Rows ?? 1) && rowCount < 1000; row++)
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

        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string fileName)
    {
        var filePath = Path.Combine(_uploadPath, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
} 