using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Services;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

/// <summary>
/// Tests for FileProcessingService
/// </summary>
public class FileProcessingServiceTests
{
    private readonly FileProcessingService _service;
    private readonly Mock<ILogger<FileProcessingService>> _mockLogger;
    private readonly Mock<IFileStorageService> _mockFileStorageService;

    public FileProcessingServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileProcessingService>>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _service = new FileProcessingService(_mockLogger.Object, _mockFileStorageService.Object);
    }

    #region ValidateFileAsync Tests

    [Fact]
    public async Task ValidateFileAsync_ShouldReturnSuccess_WhenFileIsValid()
    {
        // Arrange
        var fileName = "test.csv";
        var fileSize = 1024L;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test,data\n1,2"));

        // Act
        var result = await _service.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateFileAsync_ShouldFail_WhenFileIsEmpty()
    {
        // Arrange
        var fileName = "test.csv";
        var fileSize = 0L;
        using var stream = new MemoryStream();

        // Act
        var result = await _service.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("File is empty");
    }

    [Fact]
    public async Task ValidateFileAsync_ShouldFail_WhenFileSizeExceedsMaximum()
    {
        // Arrange
        var fileName = "test.csv";
        var fileSize = 101 * 1024 * 1024L; // 101 MB (exceeds 100 MB limit)
        using var stream = new MemoryStream();

        // Act
        var result = await _service.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum allowed size");
    }

    [Theory]
    [InlineData("test.txt")]
    [InlineData("data.csv")]
    [InlineData("config.json")]
    [InlineData("document.xml")]
    [InlineData("spreadsheet.xlsx")]
    public async Task ValidateFileAsync_ShouldReturnSuccess_ForAllowedFileTypes(string fileName)
    {
        // Arrange
        var fileSize = 1024L;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test data"));

        // Act
        var result = await _service.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("test.pdf")]
    [InlineData("document.docx")]
    [InlineData("image.png")]
    [InlineData("script.exe")]
    public async Task ValidateFileAsync_ShouldFail_ForUnsupportedFileTypes(string fileName)
    {
        // Arrange
        var fileSize = 1024L;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test data"));

        // Act
        var result = await _service.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not supported");
    }

    [Theory]
    [InlineData("../test.csv")]
    [InlineData("..\\test.csv")]
    [InlineData("folder/file.csv")]
    [InlineData("folder\\file.csv")]
    public async Task ValidateFileAsync_ShouldFail_WhenFileNameContainsPathTraversal(string fileName)
    {
        // Arrange
        var fileSize = 1024L;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test data"));

        // Act
        var result = await _service.ValidateFileAsync(stream, fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("path traversal");
    }

    #endregion

    #region ProcessFileAsync - CSV Tests

    [Fact]
    public async Task ProcessFileAsync_ShouldProcessCsvFile_Successfully()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,30,NYC\nJane,25,LA\nBob,35,Chicago";
        var filePath = CreateS3Path("test.csv");
        SetupMockFileStorage(filePath, csvContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.CSV);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(3);
        result.ColumnCount.Should().Be(3);
        result.Schema.Should().NotBeNullOrEmpty();
        result.PreviewData.Should().NotBeNullOrEmpty();

        // Verify schema contains column names
        result.Schema.Should().Contain("Name");
        result.Schema.Should().Contain("Age");
        result.Schema.Should().Contain("City");

        // Verify preview data
        result.PreviewData.Should().Contain("John");
        result.PreviewData.Should().Contain("Jane");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldFail_WhenCsvFileIsEmpty()
    {
        // Arrange
        var filePath = CreateS3Path("empty.csv");
        SetupMockFileStorage(filePath, "");

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.CSV);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleCsvWithOnlyHeader()
    {
        // Arrange
        var csvContent = "Name,Age,City";
        var filePath = CreateS3Path("header-only.csv");
        SetupMockFileStorage(filePath, csvContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.CSV);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(0);
        result.ColumnCount.Should().Be(3);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldLimitCsvPreviewToTenRows()
    {
        // Arrange
        var csvLines = new List<string> { "Name,Age" };
        for (int i = 1; i <= 15; i++)
        {
            csvLines.Add($"Person{i},{20 + i}");
        }
        var csvContent = string.Join("\n", csvLines);
        var filePath = CreateS3Path("large.csv");
        SetupMockFileStorage(filePath, csvContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.CSV);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(15);

        // Verify preview is limited to 10 rows
        var previewData = JsonDocument.Parse(result.PreviewData!);
        previewData.RootElement.GetProperty("PreviewRowCount").GetInt32().Should().Be(10);
    }

    #endregion

    #region ProcessFileAsync - JSON Tests

    [Fact]
    public async Task ProcessFileAsync_ShouldProcessJsonFile_Successfully()
    {
        // Arrange
        var jsonContent = @"[
            {""name"": ""John"", ""age"": 30, ""city"": ""NYC""},
            {""name"": ""Jane"", ""age"": 25, ""city"": ""LA""},
            {""name"": ""Bob"", ""age"": 35, ""city"": ""Chicago""}
        ]";
        var filePath = CreateS3Path("test.json");
        SetupMockFileStorage(filePath, jsonContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.JSON);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(3);
        result.ColumnCount.Should().Be(3);
        result.Schema.Should().NotBeNullOrEmpty();
        result.PreviewData.Should().NotBeNullOrEmpty();

        // Verify schema
        result.Schema.Should().Contain("name");
        result.Schema.Should().Contain("age");
        result.Schema.Should().Contain("city");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldFail_WhenJsonIsNotArray()
    {
        // Arrange
        var jsonContent = @"{""name"": ""John"", ""age"": 30}";
        var filePath = CreateS3Path("object.json");
        SetupMockFileStorage(filePath, jsonContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.JSON);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("array");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldFail_WhenJsonArrayIsEmpty()
    {
        // Arrange
        var jsonContent = "[]";
        var filePath = CreateS3Path("empty.json");
        SetupMockFileStorage(filePath, jsonContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.JSON);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldLimitJsonPreviewToTenRows()
    {
        // Arrange
        var items = new List<object>();
        for (int i = 1; i <= 15; i++)
        {
            items.Add(new { name = $"Person{i}", age = 20 + i });
        }
        var jsonContent = JsonSerializer.Serialize(items);
        var filePath = CreateS3Path("large.json");
        SetupMockFileStorage(filePath, jsonContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.JSON);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(15);

        // Verify preview is limited to 10 rows
        var previewData = JsonDocument.Parse(result.PreviewData!);
        previewData.RootElement.GetProperty("PreviewRowCount").GetInt32().Should().Be(10);
    }

    #endregion

    #region ProcessFileAsync - TXT Tests

    [Fact]
    public async Task ProcessFileAsync_ShouldProcessTextFile_Successfully()
    {
        // Arrange
        var textContent = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        var filePath = CreateS3Path("test.txt");
        SetupMockFileStorage(filePath, textContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.TXT);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(5);
        result.ColumnCount.Should().Be(1);
        result.Schema.Should().NotBeNullOrEmpty();
        result.PreviewData.Should().NotBeNullOrEmpty();

        // Verify schema has "Line" column
        result.Schema.Should().Contain("Line");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleEmptyTextFile()
    {
        // Arrange
        var filePath = CreateS3Path("empty.txt");
        SetupMockFileStorage(filePath, "");

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.TXT);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(0);
        result.ColumnCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldLimitTextFilePreviewToTenLines()
    {
        // Arrange
        var lines = Enumerable.Range(1, 15).Select(i => $"Line {i}");
        var textContent = string.Join("\n", lines);
        var filePath = CreateS3Path("large.txt");
        SetupMockFileStorage(filePath, textContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.TXT);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().Be(15);

        // Verify preview is limited to 10 lines
        var previewData = JsonDocument.Parse(result.PreviewData!);
        previewData.RootElement.GetProperty("PreviewRowCount").GetInt32().Should().Be(10);
    }

    #endregion

    #region ProcessFileAsync - XML and Excel Tests

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleXmlFile()
    {
        // Arrange
        var xmlContent = "<?xml version=\"1.0\"?><root><item>test</item></root>";
        var filePath = CreateS3Path("test.xml");
        SetupMockFileStorage(filePath, xmlContent);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.XML);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        // XML processing is simplified in current implementation
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleExcelFile()
    {
        // Arrange
        var filePath = CreateS3Path("test.xlsx");
        SetupMockFileStorage(filePath, "dummy excel content");

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.Excel);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        // Excel processing is simplified in current implementation
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ProcessFileAsync_ShouldFail_WhenFileNotFound()
    {
        // Arrange
        var nonExistentPath = CreateS3Path("nonexistent.csv");
        _mockFileStorageService
            .Setup(x => x.GetFileAsync(nonExistentPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var result = await _service.ProcessFileAsync(nonExistentPath, FileType.CSV);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldFail_ForUnsupportedFileType()
    {
        // Arrange
        var filePath = CreateS3Path("test.pdf");
        SetupMockFileStorage(filePath, "dummy content");
        var unsupportedType = FileType.FromString("PDF");

        // Act
        var result = await _service.ProcessFileAsync(filePath, unsupportedType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unsupported file type");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldFail_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{invalid json content}}}";
        var filePath = CreateS3Path("invalid.json");
        SetupMockFileStorage(filePath, invalidJson);

        // Act
        var result = await _service.ProcessFileAsync(filePath, FileType.JSON);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sets up the mock file storage service to return a stream with the provided content for the given S3 path
    /// </summary>
    private void SetupMockFileStorage(string s3FilePath, string content)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(contentBytes);

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(s3FilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
    }

    /// <summary>
    /// Creates an S3 URI for testing
    /// </summary>
    private static string CreateS3Path(string fileName) => $"s3://normaize-uploads/test/{fileName}";

    #endregion
}
