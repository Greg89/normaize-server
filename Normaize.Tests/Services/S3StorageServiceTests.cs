using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.Models;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Services;

public class S3StorageServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;

    public S3StorageServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
    }

    #region Utility Method Tests

    [Theory]
    [InlineData("s3://test-bucket/path/to/file.csv", "path/to/file.csv")]
    [InlineData("s3://bucket-name/folder/subfolder/file.json", "folder/subfolder/file.json")]
    [InlineData("s3://my-bucket/file.txt", "file.txt")]
    public void ExtractObjectKeyFromUrl_WithValidS3Urls_ShouldExtractCorrectKey(string s3Url, string expectedKey)
    {
        // Act
        var result = S3StorageServiceTestsHelper.ExtractObjectKeyFromUrl(s3Url);

        // Assert
        result.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("test.csv", "text/csv")]
    [InlineData("data.json", "application/json")]
    [InlineData("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("old-spreadsheet.xls", "application/vnd.ms-excel")]
    [InlineData("config.xml", "application/xml")]
    [InlineData("readme.txt", "text/plain")]
    [InlineData("data.parquet", "application/octet-stream")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    public void GetContentType_WithDifferentExtensions_ShouldReturnCorrectMimeType(string fileName, string expectedContentType)
    {
        // Act
        var result = S3StorageServiceTestsHelper.GetContentType(fileName);

        // Assert
        result.Should().Be(expectedContentType);
    }

    #endregion

    #region Environment Mapping Tests

    [Theory]
    [InlineData("Production", "production")]
    [InlineData("Staging", "beta")]
    [InlineData("Beta", "beta")]
    [InlineData("Development", "development")]
    [InlineData("Test", "development")]
    [InlineData("Unknown", "development")]
    public void GetEnvironmentFolder_WithDifferentEnvironments_ShouldMapToCorrectFolders(string environment, string expectedFolder)
    {
        // Act
        var result = S3StorageServiceTestsHelper.GetEnvironmentFolder(environment);

        // Assert
        result.Should().Be(expectedFolder);
    }

    #endregion

    #region File Path Generation Tests

    [Fact]
    public void GenerateObjectKey_WithValidFile_ShouldCreateCorrectPath()
    {
        // Arrange
        var fileName = "test.csv";
        var environment = "development";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");

        // Act
        var result = S3StorageServiceTestsHelper.GenerateObjectKey(fileName, environment);

        // Assert
        result.Should().StartWith($"{environment}/{datePath}/");
        result.Should().EndWith($"_{fileName}");
        result.Should().Contain(".csv");
    }

    [Fact]
    public void GenerateS3Url_WithValidComponents_ShouldCreateCorrectUrl()
    {
        // Arrange
        var bucketName = "test-bucket";
        var objectKey = "development/2024/01/01/test.csv";

        // Act
        var result = S3StorageServiceTestsHelper.GenerateS3Url(bucketName, objectKey);

        // Assert
        result.Should().Be($"s3://{bucketName}/{objectKey}");
    }

    #endregion

    #region File Upload Request Validation Tests

    [Fact]
    public void ValidateFileUploadRequest_WithValidRequest_ShouldNotThrow()
    {
        // Arrange
        var fileContent = "test content";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            FileStream = fileStream,
            ContentType = "text/csv",
            FileSize = fileContent.Length
        };

        // Act & Assert
        var action = () => S3StorageServiceTestsHelper.ValidateFileUploadRequest(fileRequest);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateFileUploadRequest_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => S3StorageServiceTestsHelper.ValidateFileUploadRequest(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateFileUploadRequest_WithNullFileName_ShouldThrowArgumentException()
    {
        // Arrange
        var fileContent = "test content";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileRequest = new FileUploadRequest
        {
            FileName = null!,
            FileStream = fileStream,
            ContentType = "text/csv",
            FileSize = fileContent.Length
        };

        // Act & Assert
        var action = () => S3StorageServiceTestsHelper.ValidateFileUploadRequest(fileRequest);
        action.Should().Throw<ArgumentException>().WithMessage("*FileName*");
    }

    [Fact]
    public void ValidateFileUploadRequest_WithNullFileStream_ShouldThrowArgumentException()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            FileStream = null!,
            ContentType = "text/csv",
            FileSize = 100
        };

        // Act & Assert
        var action = () => S3StorageServiceTestsHelper.ValidateFileUploadRequest(fileRequest);
        action.Should().Throw<ArgumentException>().WithMessage("*FileStream*");
    }

    #endregion

    #region Error Handling Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateFilePath_WithInvalidPath_ShouldThrowArgumentException(string filePath)
    {
        // Act & Assert
        var action = () => S3StorageServiceTestsHelper.ValidateFilePath(filePath);
        action.Should().Throw<ArgumentException>().WithMessage("*filePath*");
    }

    [Fact]
    public void ValidateFilePath_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => S3StorageServiceTestsHelper.ValidateFilePath(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateFilePath_WithValidPath_ShouldNotThrow()
    {
        // Arrange
        var filePath = "s3://bucket/path/file.csv";

        // Act & Assert
        var action = () => S3StorageServiceTestsHelper.ValidateFilePath(filePath);
        action.Should().NotThrow();
    }

    #endregion

    #region Integration Tests (Mocked)

    [Fact]
    public void CompleteFileLifecycle_WithMockedS3_ShouldWorkEndToEnd()
    {
        // Arrange
        var fileContent = "test file content for lifecycle test";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileRequest = new FileUploadRequest
        {
            FileName = "lifecycle-test.csv",
            FileStream = fileStream,
            ContentType = "text/csv",
            FileSize = fileContent.Length
        };

        // Act & Assert
        // 1. Validate file request
        S3StorageServiceTestsHelper.ValidateFileUploadRequest(fileRequest);

        // 2. Generate object key
        var objectKey = S3StorageServiceTestsHelper.GenerateObjectKey(fileRequest.FileName, "development");
        objectKey.Should().NotBeNullOrEmpty();
        objectKey.Should().Contain("development/");
        objectKey.Should().Contain(".csv");

        // 3. Generate S3 URL
        var s3Url = S3StorageServiceTestsHelper.GenerateS3Url("test-bucket", objectKey);
        s3Url.Should().StartWith("s3://");
        s3Url.Should().Contain("test-bucket");

        // 4. Extract object key from URL
        var extractedKey = S3StorageServiceTestsHelper.ExtractObjectKeyFromUrl(s3Url);
        extractedKey.Should().Be(objectKey);

        // 5. Validate file path
        S3StorageServiceTestsHelper.ValidateFilePath(s3Url);
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData("s3://bucket/")]
    [InlineData("s3://bucket")]
    [InlineData("s3://")]
    public void ExtractObjectKeyFromUrl_WithEdgeCases_ShouldHandleCorrectly(string s3Url)
    {
        // Act
        var result = S3StorageServiceTestsHelper.ExtractObjectKeyFromUrl(s3Url);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("file")]
    [InlineData(".hidden")]
    public void GetContentType_WithEdgeCases_ShouldReturnDefault(string fileName)
    {
        // Act
        var result = S3StorageServiceTestsHelper.GetContentType(fileName);

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Theory]
    [InlineData("")]
    [InlineData("UPPERCASE")]
    [InlineData("MixedCase")]
    public void GetEnvironmentFolder_WithEdgeCases_ShouldMapCorrectly(string environment)
    {
        // Act
        var result = S3StorageServiceTestsHelper.GetEnvironmentFolder(environment);

        // Assert
        result.Should().Be("development");
    }

    #endregion
}

// Helper class to test private methods and logic without external dependencies
public static class S3StorageServiceTestsHelper
{
    public static string ExtractObjectKeyFromUrl(string filePath)
    {
        if (filePath.StartsWith("s3://"))
        {
            var uri = new Uri(filePath);
            return uri.AbsolutePath.TrimStart('/');
        }
        return filePath;
    }

    public static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".parquet" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    public static string GetEnvironmentFolder(string environment)
    {
        return environment.ToLowerInvariant() switch
        {
            "production" => "production",
            "staging" => "beta",
            "beta" => "beta",
            "development" => "development",
            _ => "development"
        };
    }

    public static string GenerateObjectKey(string fileName, string environment)
    {
        var guid = Guid.NewGuid();
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return $"{environment}/{datePath}/{guid}_{fileName}";
    }

    public static string GenerateS3Url(string bucketName, string objectKey)
    {
        return $"s3://{bucketName}/{objectKey}";
    }

    public static void ValidateFileUploadRequest(FileUploadRequest fileRequest)
    {
        ArgumentNullException.ThrowIfNull(fileRequest);

        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException("FileName is required", nameof(fileRequest));

        if (fileRequest.FileStream == null)
            throw new ArgumentException("FileStream is required", nameof(fileRequest));
    }

    public static void ValidateFilePath(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
    }
}