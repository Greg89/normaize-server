using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.DataNormalization.Infrastructure.Configuration;
using Normaize.DataNormalization.Infrastructure.Services;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

public class FileValidationServiceTests : IDisposable
{
    private readonly Mock<ILogger<FileValidationService>> _mockLogger;
    private readonly FileUploadOptions _options;
    private readonly FileValidationService _service;

    public FileValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileValidationService>>();
        _options = new FileUploadOptions
        {
            MaxFileSizeBytes = 10 * 1024 * 1024, // 10 MB
            AllowedExtensions = new List<string> { ".csv", ".json", ".xml", ".xlsx", ".txt" },
            BlockedExtensions = new List<string> { ".exe", ".bat", ".cmd", ".ps1", ".sh" }
        };

        var optionsWrapper = Options.Create(_options);
        _service = new FileValidationService(_mockLogger.Object, optionsWrapper);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region ValidateFileAsync Tests

    [Fact]
    public async Task ValidateFileAsync_ShouldSucceed_WhenFileIsValid()
    {
        // Arrange
        var fileName = "test-data.csv";
        var fileSize = 1024L;

        // Act
        var result = await _service.ValidateFileAsync(fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateFileAsync_ShouldFail_WhenFileNameIsEmpty(string? fileName)
    {
        // Arrange
        var fileSize = 1024L;

        // Act
        var result = await _service.ValidateFileAsync(fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("File name is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public async Task ValidateFileAsync_ShouldFail_WhenFileSizeIsInvalid(long fileSize)
    {
        // Arrange
        var fileName = "test.csv";

        // Act
        var result = await _service.ValidateFileAsync(fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("File size must be greater than zero");
    }

    [Theory]
    [InlineData("../test.csv")]
    [InlineData("..\\test.csv")]
    [InlineData("folder/test.csv")]
    [InlineData("folder\\test.csv")]
    public async Task ValidateFileAsync_ShouldFail_WhenFileNameContainsPathTraversal(string fileName)
    {
        // Arrange
        var fileSize = 1024L;

        // Act
        var result = await _service.ValidateFileAsync(fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("path traversal");
    }

    [Fact]
    public async Task ValidateFileAsync_ShouldFail_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var fileName = "large-file.csv";
        var fileSize = 11 * 1024 * 1024L; // 11 MB (exceeds 10 MB limit)

        // Act
        var result = await _service.ValidateFileAsync(fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum allowed size");
        result.Error.Should().Contain("10 MB");
    }

    [Theory]
    [InlineData("test.exe")]
    [InlineData("script.bat")]
    [InlineData("malware.cmd")]
    [InlineData("script.ps1")]
    public async Task ValidateFileAsync_ShouldFail_WhenFileExtensionIsBlocked(string fileName)
    {
        // Arrange
        var fileSize = 1024L;

        // Act
        var result = await _service.ValidateFileAsync(fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not supported");
    }

    [Theory]
    [InlineData("test.pdf")]
    [InlineData("document.docx")]
    [InlineData("image.png")]
    public async Task ValidateFileAsync_ShouldFail_WhenFileExtensionIsNotAllowed(string fileName)
    {
        // Arrange
        var fileSize = 1024L;

        // Act
        var result = await _service.ValidateFileAsync(fileName, fileSize);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not supported");
        result.Error.Should().Contain("Allowed types");
    }

    #endregion

    #region IsFileSizeValid Tests

    [Theory]
    [InlineData(1024)]
    [InlineData(5 * 1024 * 1024)]
    [InlineData(10 * 1024 * 1024)]
    public void IsFileSizeValid_ShouldReturnTrue_WhenSizeIsWithinLimit(long fileSize)
    {
        // Act
        var result = _service.IsFileSizeValid(fileSize);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11 * 1024 * 1024)]
    [InlineData(100 * 1024 * 1024)]
    public void IsFileSizeValid_ShouldReturnFalse_WhenSizeIsInvalid(long fileSize)
    {
        // Act
        var result = _service.IsFileSizeValid(fileSize);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFileSizeValid_ShouldUseCustomMaxSize_WhenProvided()
    {
        // Arrange
        var fileSize = 5 * 1024 * 1024L;
        var customMaxSize = 3 * 1024 * 1024L;

        // Act
        var result = _service.IsFileSizeValid(fileSize, customMaxSize);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsFileExtensionValid Tests

    [Theory]
    [InlineData("data.csv")]
    [InlineData("config.json")]
    [InlineData("document.xml")]
    [InlineData("spreadsheet.xlsx")]
    [InlineData("notes.txt")]
    public void IsFileExtensionValid_ShouldReturnTrue_WhenExtensionIsAllowed(string fileName)
    {
        // Act
        var result = _service.IsFileExtensionValid(fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("DATA.CSV")]
    [InlineData("Config.JSON")]
    [InlineData("DOCUMENT.XML")]
    public void IsFileExtensionValid_ShouldBeCaseInsensitive(string fileName)
    {
        // Act
        var result = _service.IsFileExtensionValid(fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.bat")]
    [InlineData("command.cmd")]
    public void IsFileExtensionValid_ShouldReturnFalse_WhenExtensionIsBlocked(string fileName)
    {
        // Act
        var result = _service.IsFileExtensionValid(fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("image.png")]
    [InlineData("video.mp4")]
    public void IsFileExtensionValid_ShouldReturnFalse_WhenExtensionIsNotAllowed(string fileName)
    {
        // Act
        var result = _service.IsFileExtensionValid(fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsFileExtensionValid_ShouldReturnFalse_WhenFileNameIsEmpty(string? fileName)
    {
        // Act
        var result = _service.IsFileExtensionValid(fileName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsFileNameSafe Tests

    [Theory]
    [InlineData("test.csv")]
    [InlineData("my-data-file.json")]
    [InlineData("Report_2024.xlsx")]
    [InlineData("file with spaces.txt")]
    public void IsFileNameSafe_ShouldReturnTrue_WhenFileNameIsSafe(string fileName)
    {
        // Act
        var result = _service.IsFileNameSafe(fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\windows\\system32")]
    [InlineData("folder/file.csv")]
    [InlineData("folder\\file.csv")]
    [InlineData("../../secret.txt")]
    public void IsFileNameSafe_ShouldReturnFalse_WhenFileNameContainsPathTraversal(string fileName)
    {
        // Act
        var result = _service.IsFileNameSafe(fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsFileNameSafe_ShouldReturnFalse_WhenFileNameIsEmpty(string? fileName)
    {
        // Act
        var result = _service.IsFileNameSafe(fileName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetFileExtension Tests

    [Theory]
    [InlineData("test.csv", ".csv")]
    [InlineData("data.json", ".json")]
    [InlineData("FILE.XML", ".xml")]
    [InlineData("document.XLSX", ".xlsx")]
    public void GetFileExtension_ShouldReturnLowercaseExtension(string fileName, string expected)
    {
        // Act
        var result = _service.GetFileExtension(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFileExtension_ShouldReturnEmptyString_WhenFileNameIsEmpty(string? fileName)
    {
        // Act
        var result = _service.GetFileExtension(fileName);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetFileExtension_ShouldReturnEmptyString_WhenNoExtension()
    {
        // Arrange
        var fileName = "noextension";

        // Act
        var result = _service.GetFileExtension(fileName);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Configuration Getter Tests

    [Fact]
    public void GetAllowedExtensions_ShouldReturnConfiguredExtensions()
    {
        // Act
        var result = _service.GetAllowedExtensions();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Should().Contain(new[] { ".csv", ".json", ".xml", ".xlsx", ".txt" });
    }

    [Fact]
    public void GetBlockedExtensions_ShouldReturnConfiguredExtensions()
    {
        // Act
        var result = _service.GetBlockedExtensions();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Should().Contain(new[] { ".exe", ".bat", ".cmd", ".ps1", ".sh" });
    }

    [Fact]
    public void GetMaxFileSize_ShouldReturnConfiguredSize()
    {
        // Act
        var result = _service.GetMaxFileSize();

        // Assert
        result.Should().Be(10 * 1024 * 1024);
    }

    #endregion
}
