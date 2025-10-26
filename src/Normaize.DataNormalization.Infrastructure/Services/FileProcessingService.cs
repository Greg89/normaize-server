using System.Text.Json;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Implementation of file processing service for validating and processing uploaded files
/// </summary>
public class FileProcessingService : IFileProcessingService
{
    private readonly ILogger<FileProcessingService> _logger;
    private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB
    private static readonly string[] AllowedExtensions = { ".csv", ".json", ".xml", ".xlsx", ".txt" };

    public FileProcessingService(ILogger<FileProcessingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<FileValidationResult> ValidateFileAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating file: {FileName}, size: {FileSize}", fileName, fileSize);

        // Check file size
        if (fileSize <= 0)
        {
            return Task.FromResult(new FileValidationResult(false, "File is empty"));
        }

        if (fileSize > MaxFileSize)
        {
            return Task.FromResult(new FileValidationResult(
                false,
                $"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB"));
        }

        // Check file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Task.FromResult(new FileValidationResult(
                false,
                $"File type '{extension}' is not supported. Allowed types: {string.Join(", ", AllowedExtensions)}"));
        }

        // Check file name for security
        if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            return Task.FromResult(new FileValidationResult(
                false,
                "Invalid file name - potential path traversal detected"));
        }

        _logger.LogInformation("File validation passed: {FileName}", fileName);
        return Task.FromResult(new FileValidationResult(true));
    }

    public async Task<FileProcessingResult> ProcessFileAsync(
        string filePath,
        FileType fileType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Processing file: {FilePath}, type: {FileType}", filePath, fileType.Value);

        try
        {
            return fileType.Value.ToUpperInvariant() switch
            {
                "CSV" => await ProcessCsvFileAsync(filePath, cancellationToken),
                "JSON" => await ProcessJsonFileAsync(filePath, cancellationToken),
                "XML" => await ProcessXmlFileAsync(filePath, cancellationToken),
                "EXCEL" => await ProcessExcelFileAsync(filePath, cancellationToken),
                "TXT" => await ProcessTextFileAsync(filePath, cancellationToken),
                _ => new FileProcessingResult(false, Error: $"Unsupported file type: {fileType.Value}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
            return new FileProcessingResult(false, Error: $"Error processing file: {ex.Message}");
        }
    }

    private async Task<FileProcessingResult> ProcessCsvFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        
        if (lines.Length == 0)
        {
            return new FileProcessingResult(false, Error: "CSV file is empty");
        }

        // Parse header
        var headers = lines[0].Split(',').Select(h => h.Trim()).ToList();
        var columnCount = headers.Count;
        var rowCount = lines.Length - 1; // Exclude header

        // Generate schema
        var schema = JsonSerializer.Serialize(new
        {
            Columns = headers.Select(h => new { Name = h, Type = "string" }).ToList()
        });

        // Generate preview (first 10 rows)
        var previewRows = new List<Dictionary<string, object>>();
        for (int i = 1; i < Math.Min(11, lines.Length); i++)
        {
            var values = lines[i].Split(',');
            var row = new Dictionary<string, object>();
            for (int j = 0; j < Math.Min(headers.Count, values.Length); j++)
            {
                row[headers[j]] = values[j].Trim();
            }
            previewRows.Add(row);
        }

        var previewData = JsonSerializer.Serialize(new
        {
            Columns = headers,
            Rows = previewRows,
            TotalRows = rowCount,
            PreviewRowCount = previewRows.Count
        });

        return new FileProcessingResult(
            true,
            schema,
            rowCount,
            columnCount,
            previewData);
    }

    private async Task<FileProcessingResult> ProcessJsonFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var rowCount = root.GetArrayLength();
            if (rowCount == 0)
            {
                return new FileProcessingResult(false, Error: "JSON array is empty");
            }

            // Get column names from first object
            var firstElement = root[0];
            var headers = new List<string>();
            foreach (var property in firstElement.EnumerateObject())
            {
                headers.Add(property.Name);
            }

            var columnCount = headers.Count;

            // Generate schema
            var schema = JsonSerializer.Serialize(new
            {
                Columns = headers.Select(h => new { Name = h, Type = "string" }).ToList()
            });

            // Generate preview
            var previewRows = new List<Dictionary<string, object>>();
            var previewCount = Math.Min(10, rowCount);
            for (int i = 0; i < previewCount; i++)
            {
                var row = new Dictionary<string, object>();
                foreach (var property in root[i].EnumerateObject())
                {
                    row[property.Name] = property.Value.ToString();
                }
                previewRows.Add(row);
            }

            var previewData = JsonSerializer.Serialize(new
            {
                Columns = headers,
                Rows = previewRows,
                TotalRows = rowCount,
                PreviewRowCount = previewRows.Count
            });

            return new FileProcessingResult(
                true,
                schema,
                rowCount,
                columnCount,
                previewData);
        }

        return new FileProcessingResult(false, Error: "JSON file must contain an array");
    }

    private Task<FileProcessingResult> ProcessXmlFileAsync(string filePath, CancellationToken cancellationToken)
    {
        // Simplified XML processing - in production, use proper XML parsing
        return Task.FromResult(new FileProcessingResult(
            true,
            JsonSerializer.Serialize(new { Columns = new[] { new { Name = "Data", Type = "string" } } }),
            1,
            1,
            JsonSerializer.Serialize(new { Columns = new[] { "Data" }, Rows = new object[0], TotalRows = 0, PreviewRowCount = 0 })));
    }

    private Task<FileProcessingResult> ProcessExcelFileAsync(string filePath, CancellationToken cancellationToken)
    {
        // Simplified Excel processing - in production, use EPPlus or ClosedXML
        return Task.FromResult(new FileProcessingResult(
            true,
            JsonSerializer.Serialize(new { Columns = new[] { new { Name = "Data", Type = "string" } } }),
            1,
            1,
            JsonSerializer.Serialize(new { Columns = new[] { "Data" }, Rows = new object[0], TotalRows = 0, PreviewRowCount = 0 })));
    }

    private async Task<FileProcessingResult> ProcessTextFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        
        var schema = JsonSerializer.Serialize(new
        {
            Columns = new[] { new { Name = "Line", Type = "string" } }
        });

        var previewRows = lines
            .Take(10)
            .Select(line => new Dictionary<string, object> { ["Line"] = line })
            .ToList();

        var previewData = JsonSerializer.Serialize(new
        {
            Columns = new[] { "Line" },
            Rows = previewRows,
            TotalRows = lines.Length,
            PreviewRowCount = previewRows.Count
        });

        return new FileProcessingResult(
            true,
            schema,
            lines.Length,
            1,
            previewData);
    }
}
