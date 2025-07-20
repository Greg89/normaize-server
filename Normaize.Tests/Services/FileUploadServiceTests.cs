using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Services;
using System.Text;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Configuration.Memory;
using Normaize.Core.Configuration;

namespace Normaize.Tests.Services;

public class FileUploadServiceTests
{
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<IDataProcessingInfrastructure> _mockInfrastructure;
    private readonly FileUploadService _service;
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly DataProcessingConfiguration _dataProcessingConfig;

    public FileUploadServiceTests()
    {
        _mockStorageService = new Mock<IStorageService>();
        _mockInfrastructure = new Mock<IDataProcessingInfrastructure>();

        // Setup configuration objects
        _fileUploadConfig = new FileUploadConfiguration
        {
            MaxFileSize = 10485760, // 10MB
            AllowedExtensions = new[] { ".csv", ".json", ".xlsx", ".xls", ".xml", ".parquet", ".txt" },
            MaxPreviewRows = 100, // Must be at least 100 (matches new default)
            MaxConcurrentUploads = 5,
            EnableCompression = true,
            BlockedExtensions = new[] { ".exe", ".bat", ".cmd", ".ps1", ".sh", ".dll", ".so", ".dylib" }
        };

        _dataProcessingConfig = new DataProcessingConfiguration
        {
            MaxRowsPerDataset = 10000,
            MaxColumnsPerDataset = 100,
            MaxPreviewRows = 10, // Must be between 1-100
            EnableDataValidation = true,
            EnableSchemaInference = true,
            MaxProcessingTimeSeconds = 30
        };

        var mockFileUploadOptions = new Mock<IOptions<FileUploadConfiguration>>();
        mockFileUploadOptions.Setup(x => x.Value).Returns(_fileUploadConfig);

        var mockDataProcessingOptions = new Mock<IOptions<DataProcessingConfiguration>>();
        mockDataProcessingOptions.Setup(x => x.Value).Returns(_dataProcessingConfig);

        // Setup infrastructure mocks
        SetupInfrastructureMocks();

        _service = new FileUploadService(
            mockFileUploadOptions.Object,
            mockDataProcessingOptions.Object,
            _mockStorageService.Object,
            _mockInfrastructure.Object);
    }

    #region SaveFileAsync Tests

    [Fact]
    public async Task SaveFileAsync_ValidRequest_ReturnsFilePath()
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        var expectedPath = "uploads/test.csv";
        _mockStorageService.Setup(x => x.SaveFileAsync(fileRequest)).ReturnsAsync(expectedPath);

        // Act
        var result = await _service.SaveFileAsync(fileRequest);

        // Assert
        Assert.Equal(expectedPath, result);
        _mockStorageService.Verify(x => x.SaveFileAsync(fileRequest), Times.Once);
    }

    [Fact]
    public async Task SaveFileAsync_StorageServiceThrowsException_PropagatesException()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            FileSize = 1024,
            FileStream = new MemoryStream(Encoding.UTF8.GetBytes("test"))
        };
        var expectedException = new InvalidOperationException("Storage error");
        _mockStorageService.Setup(x => x.SaveFileAsync(fileRequest)).ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileUploadException>(() => 
            _service.SaveFileAsync(fileRequest));
        Assert.Contains("Failed to save file test.csv", exception.Message);
        Assert.Equal(expectedException, exception.InnerException);
    }

    #endregion

    #region ValidateFileAsync Tests

    [Fact]
    public async Task ValidateFileAsync_FileSizeExceedsLimit_ReturnsFalse()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "large.csv",
            FileSize = 20000000 // 20MB, exceeds 10MB limit
        };

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateFileAsync_BlockedExtension_ReturnsFalse()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "malicious.exe",
            FileSize = 1024
        };

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            FileSize = 1024
        };

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".doc")]
    [InlineData(".exe")]
    [InlineData(".zip")]
    public async Task ValidateFileAsync_InvalidExtension_ReturnsFalse(string invalidExtension)
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        fileRequest.FileName = $"test{invalidExtension}";

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(".csv")]
    [InlineData(".json")]
    [InlineData(".xlsx")]
    [InlineData(".xls")]
    [InlineData(".xml")]
    [InlineData(".parquet")]
    [InlineData(".txt")]
    public async Task ValidateFileAsync_ValidExtension_ReturnsTrue(string validExtension)
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        fileRequest.FileName = $"test{validExtension}";

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ProcessFileAsync Tests

    [Fact]
    public async Task ProcessFileAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = "nonexistent.csv";
        var fileType = ".csv";
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _service.ProcessFileAsync(filePath, fileType));
        Assert.Contains("File not found", exception.Message);
    }

    [Fact]
    public async Task ProcessFileAsync_UnsupportedFileType_ThrowsUnsupportedFileTypeException()
    {
        // Arrange
        var filePath = "test.unsupported";
        var fileType = ".unsupported";
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnsupportedFileTypeException>(() => 
            _service.ProcessFileAsync(filePath, fileType));
        Assert.Contains("File type .unsupported is not supported", exception.Message);
    }

    [Fact]
    public async Task ProcessFileAsync_ProcessingError_SetsErrorProperties()
    {
        // Arrange
        var filePath = "test.csv";
        var fileType = ".csv";
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ThrowsAsync(new IOException("Read error"));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        Assert.False(result.IsProcessed);
        Assert.Contains("Read error", result.ProcessingErrors);
        Assert.NotNull(result.ProcessingErrors);
    }

    #endregion

    #region CSV Processing Tests

    [Fact]
    public async Task ProcessFileAsync_CsvFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.csv";
        var fileType = ".csv";
        var csvContent = "Name,Age,City\nJohn,30,New York\nJane,25,Boston";
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ReturnsAsync(() => new MemoryStream(csvBytes));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        if (!result.IsProcessed)
        {
            // Output the error for debugging
            System.Diagnostics.Debug.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"Processing error: {result.ProcessingErrors}");
        }
        Assert.True(result.IsProcessed);
        Assert.Equal("test.csv", result.FileName);
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal(FileType.CSV, result.FileType);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(3, result.ColumnCount);
        Assert.NotNull(result.Schema);
        Assert.NotNull(result.PreviewData);
        Assert.NotNull(result.ProcessedData);
        Assert.NotNull(result.DataHash);
    }

    [Fact]
    public async Task ProcessFileAsync_CsvFileWithManyRows_UsesSeparateTable()
    {
        // Arrange
        var filePath = "large.csv";
        var fileType = ".csv";
        var csvContent = "Name,Age\n" + string.Join("\n", Enumerable.Range(1, 15000).Select(i => $"User{i},{i}"));
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ReturnsAsync(() => new MemoryStream(csvBytes));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        if (!result.UseSeparateTable)
        {
            System.Diagnostics.Debug.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"RowCount: {result.RowCount}, MaxRows: {10000}");
            System.Console.WriteLine($"FileSize: {result.FileSize}, MaxFileSize: {10485760}");
        }
        Assert.True(result.UseSeparateTable);
        Assert.Equal(10000, result.RowCount); // Limited by maxRows
        Assert.Null(result.ProcessedData); // Should not store inline
        Assert.NotNull(result.PreviewData); // Should still have preview
    }

    #endregion

    #region JSON Processing Tests

    [Fact]
    public async Task ProcessFileAsync_JsonArray_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.json";
        var fileType = ".json";
        var jsonContent = @"[
            {""name"": ""John"", ""age"": 30, ""city"": ""New York""},
            {""name"": ""Jane"", ""age"": 25, ""city"": ""Boston""}
        ]";
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
        
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ReturnsAsync(() => new MemoryStream(jsonBytes));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        if (!result.IsProcessed)
        {
            System.Diagnostics.Debug.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"Processing error: {result.ProcessingErrors}");
        }
        Assert.True(result.IsProcessed);
        Assert.Equal(FileType.JSON, result.FileType);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(3, result.ColumnCount);
        Assert.NotNull(result.Schema);
        Assert.NotNull(result.PreviewData);
        Assert.NotNull(result.ProcessedData);
    }

    [Fact]
    public async Task ProcessFileAsync_JsonObject_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.json";
        var fileType = ".json";
        var jsonContent = @"{""name"": ""John"", ""age"": 30, ""city"": ""New York""}";
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
        
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ReturnsAsync(() => new MemoryStream(jsonBytes));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        if (!result.IsProcessed)
        {
            System.Diagnostics.Debug.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"Processing error: {result.ProcessingErrors}");
        }
        Assert.True(result.IsProcessed);
        Assert.Equal(FileType.JSON, result.FileType);
        Assert.Equal(1, result.RowCount);
        Assert.Equal(3, result.ColumnCount);
        Assert.NotNull(result.Schema);
        Assert.NotNull(result.PreviewData);
        Assert.NotNull(result.ProcessedData);
    }

    #endregion

    #region Excel Processing Tests

    [Fact]
    public async Task ProcessFileAsync_ExcelFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.xlsx";
        var fileType = ".xlsx";
        var excelBytes = CreateSampleExcelFile();
        
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ReturnsAsync(() => new MemoryStream(excelBytes));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        if (!result.IsProcessed)
        {
            System.Diagnostics.Debug.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"Processing error: {result.ProcessingErrors}");
        }
        Assert.True(result.IsProcessed);
        Assert.Equal(FileType.Excel, result.FileType);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(3, result.ColumnCount);
        Assert.NotNull(result.Schema);
        Assert.NotNull(result.PreviewData);
        Assert.NotNull(result.ProcessedData);
    }

    #endregion

    #region XML Processing Tests

    [Fact]
    public async Task ProcessFileAsync_XmlFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.xml";
        var fileType = ".xml";
        var xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
    <item>
        <name>John</name>
        <age>30</age>
        <city>New York</city>
    </item>
    <item>
        <name>Jane</name>
        <age>25</age>
        <city>Boston</city>
    </item>
</root>";
        var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);
        
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ReturnsAsync(() => new MemoryStream(xmlBytes));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        if (!result.IsProcessed)
        {
            System.Diagnostics.Debug.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"Processing error: {result.ProcessingErrors}");
        }
        Assert.True(result.IsProcessed);
        Assert.Equal(FileType.XML, result.FileType);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(3, result.ColumnCount);
        Assert.NotNull(result.Schema);
        Assert.NotNull(result.PreviewData);
        Assert.NotNull(result.ProcessedData);
    }

    #endregion

    #region TXT Processing Tests

    [Fact]
    public async Task ProcessFileAsync_TextFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.txt";
        var fileType = ".txt";
        var textContent = "Line 1\nLine 2\nLine 3";
        var textBytes = Encoding.UTF8.GetBytes(textContent);
        
        _mockStorageService.Setup(x => x.FileExistsAsync(filePath)).ReturnsAsync(true);
        _mockStorageService.Setup(x => x.GetFileAsync(filePath)).ReturnsAsync(() => new MemoryStream(textBytes));

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        if (!result.IsProcessed)
        {
            System.Diagnostics.Debug.WriteLine($"Processing error: {result.ProcessingErrors}");
            System.Console.WriteLine($"Processing error: {result.ProcessingErrors}");
        }
        Assert.True(result.IsProcessed);
        Assert.Equal(FileType.TXT, result.FileType);
        Assert.Equal(3, result.RowCount);
        Assert.Equal(2, result.ColumnCount); // LineNumber and Content columns
        Assert.NotNull(result.Schema);
        Assert.NotNull(result.PreviewData);
        Assert.NotNull(result.ProcessedData);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_ValidPath_CallsStorageService()
    {
        // Arrange
        var filePath = "test.csv";
        _mockStorageService.Setup(x => x.DeleteFileAsync(filePath)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        _mockStorageService.Verify(x => x.DeleteFileAsync(filePath), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_StorageServiceThrowsException_LogsError()
    {
        // Arrange
        var filePath = "test.csv";
        var expectedException = new InvalidOperationException("Delete error");
        _mockStorageService.Setup(x => x.DeleteFileAsync(filePath)).ThrowsAsync(expectedException);

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        // The service catches and logs exceptions, doesn't re-throw them
        _mockStorageService.Verify(x => x.DeleteFileAsync(filePath), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupInfrastructureMocks()
    {
        // Setup logger mock
        var mockLogger = new Mock<ILogger<FileUploadService>>();
        _mockInfrastructure.Setup(x => x.Logger).Returns(mockLogger.Object);

        // Setup structured logging mock
        var mockStructuredLogging = new Mock<IStructuredLoggingService>();
        _mockInfrastructure.Setup(x => x.StructuredLogging).Returns(mockStructuredLogging.Object);

        // Setup chaos engineering mock
        var mockChaosEngineering = new Mock<IChaosEngineeringService>();
        _mockInfrastructure.Setup(x => x.ChaosEngineering).Returns(mockChaosEngineering.Object);

        // Setup default timeout values
        _mockInfrastructure.Setup(x => x.DefaultTimeout).Returns(TimeSpan.FromSeconds(30));
        _mockInfrastructure.Setup(x => x.QuickTimeout).Returns(TimeSpan.FromSeconds(5));

        // Setup structured logging context creation
        mockStructuredLogging.Setup(s => s.CreateContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns<string, string, string, Dictionary<string, object>>((operationName, correlationId, userId, metadata) =>
            {
                var mockContext = new Mock<IOperationContext>();
                mockContext.Setup(c => c.OperationName).Returns(operationName);
                mockContext.Setup(c => c.CorrelationId).Returns(correlationId);
                mockContext.Setup(c => c.UserId).Returns(userId);
                mockContext.Setup(c => c.Metadata).Returns(metadata ?? new Dictionary<string, object>());
                mockContext.Setup(c => c.Steps).Returns(new List<string>());
                mockContext.Setup(c => c.Stopwatch).Returns(System.Diagnostics.Stopwatch.StartNew());
                return mockContext.Object;
            });

        // Setup chaos engineering execution
        mockChaosEngineering.Setup(x => x.ExecuteChaosAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>(), It.IsAny<Dictionary<string, object>>()))
            .Returns<string, string, string, Func<Task>, Dictionary<string, object>>(async (scenario, correlationId, operationName, operation, metadata) =>
            {
                await operation();
                return true;
            });
    }

    private static FileUploadRequest CreateValidFileUploadRequest()
    {
        return new FileUploadRequest
        {
            FileName = "test.csv",
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream(Encoding.UTF8.GetBytes("test content")),
            Description = "Test file",
            Tags = "test,csv",
            KeepOriginalFile = true,
            StoreProcessedData = true
        };
    }

    private static byte[] CreateSampleExcelFile()
    {
        // Create a simple Excel file for testing
        using var package = new OfficeOpenXml.ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");
        
        // Add headers
        worksheet.Cells[1, 1].Value = "Name";
        worksheet.Cells[1, 2].Value = "Age";
        worksheet.Cells[1, 3].Value = "City";
        
        // Add data
        worksheet.Cells[2, 1].Value = "John";
        worksheet.Cells[2, 2].Value = 30;
        worksheet.Cells[2, 3].Value = "New York";
        
        worksheet.Cells[3, 1].Value = "Jane";
        worksheet.Cells[3, 2].Value = 25;
        worksheet.Cells[3, 3].Value = "Boston";
        
        return package.GetAsByteArray();
    }

    #endregion
} 