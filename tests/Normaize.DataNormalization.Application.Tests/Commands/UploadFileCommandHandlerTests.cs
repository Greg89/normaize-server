using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Commands.FileUpload;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.ValueObjects;
using System.Text;

namespace Normaize.DataNormalization.Application.Tests.Commands;

public class UploadFileCommandHandlerTests : IDisposable
{
    private readonly Mock<IFileValidationService> _mockValidationService;
    private readonly Mock<IFileStorageService> _mockStorageService;
    private readonly Mock<IFileProcessingService> _mockProcessingService;
    private readonly Mock<ILogger<UploadFileCommandHandler>> _mockLogger;
    private readonly UploadFileCommandHandler _handler;

    public UploadFileCommandHandlerTests()
    {
        _mockValidationService = new Mock<IFileValidationService>();
        _mockStorageService = new Mock<IFileStorageService>();
        _mockProcessingService = new Mock<IFileProcessingService>();
        _mockLogger = new Mock<ILogger<UploadFileCommandHandler>>();

        _handler = new UploadFileCommandHandler(
            _mockValidationService.Object,
            _mockStorageService.Object,
            _mockProcessingService.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Successful Upload Tests

    [Fact]
    public async Task Handle_ShouldUploadAndProcessFile_WhenValidAndProcessImmediately()
    {
        // Arrange
        var fileName = "test-data.csv";
        var fileContent = "Name,Age\nJohn,30\nJane,25";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var fileSize = stream.Length;

        var command = new UploadFileCommand
        {
            FileName = fileName,
            FileStream = stream,
            FileSize = fileSize,
            UserId = "user123",
            ProcessImmediately = true
        };

        _mockValidationService
            .Setup(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(true));

        _mockValidationService
            .Setup(x => x.GetFileExtension(fileName))
            .Returns(".csv");

        _mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, "user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/user123/test-data.csv");

        _mockProcessingService
            .Setup(x => x.ProcessFileAsync(
                It.IsAny<string>(),
                It.IsAny<FileType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.FileProcessingResult(
                true,
                "{\"columns\":[\"Name\",\"Age\"]}",
                2,
                2,
                "{\"preview\":\"data\"}"
            ));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("/uploads/user123/test-data.csv");
        result.FileId.Should().Be("test-data.csv");
        result.ProcessingResult.Should().NotBeNull();
        result.ProcessingResult!.IsSuccess.Should().BeTrue();
        result.ProcessingResult.RowCount.Should().Be(2);
        result.ProcessingResult.ColumnCount.Should().Be(2);
        result.Error.Should().BeNull();

        _mockValidationService.Verify(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()), Times.Once);
        _mockStorageService.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, "user123", It.IsAny<CancellationToken>()), Times.Once);
        _mockProcessingService.Verify(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUploadWithoutProcessing_WhenProcessImmediatelyIsFalse()
    {
        // Arrange
        var fileName = "test-data.json";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("[]"));
        var fileSize = stream.Length;

        var command = new UploadFileCommand
        {
            FileName = fileName,
            FileStream = stream,
            FileSize = fileSize,
            UserId = "user123",
            ProcessImmediately = false
        };

        _mockValidationService
            .Setup(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(true));

        _mockValidationService
            .Setup(x => x.GetFileExtension(fileName))
            .Returns(".json");

        _mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, "user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/user123/test-data.json");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("/uploads/user123/test-data.json");
        result.ProcessingResult.Should().BeNull();
        result.Error.Should().BeNull();

        _mockProcessingService.Verify(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<FileType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task Handle_ShouldReturnError_WhenValidationFails()
    {
        // Arrange
        var fileName = "invalid-file.exe";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var fileSize = stream.Length;

        var command = new UploadFileCommand
        {
            FileName = fileName,
            FileStream = stream,
            FileSize = fileSize,
            UserId = "user123"
        };

        _mockValidationService
            .Setup(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(false, "File type '.exe' is not supported"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not supported");
        result.FilePath.Should().BeNull();
        result.ProcessingResult.Should().BeNull();

        _mockStorageService.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var fileName = "large-file.csv";
        var stream = new MemoryStream(new byte[100]);
        var fileSize = 100 * 1024 * 1024L; // 100 MB

        var command = new UploadFileCommand
        {
            FileName = fileName,
            FileStream = stream,
            FileSize = fileSize,
            UserId = "user123"
        };

        _mockValidationService
            .Setup(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(false, "File size exceeds maximum allowed size"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    #endregion

    #region Storage Failure Tests

    [Fact]
    public async Task Handle_ShouldReturnError_WhenStorageFails()
    {
        // Arrange
        var fileName = "test-data.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var fileSize = stream.Length;

        var command = new UploadFileCommand
        {
            FileName = fileName,
            FileStream = stream,
            FileSize = fileSize,
            UserId = "user123"
        };

        _mockValidationService
            .Setup(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(true));

        _mockValidationService
            .Setup(x => x.GetFileExtension(fileName))
            .Returns(".csv");

        _mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, "user123", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Upload failed");
        result.Error.Should().Contain("Disk full");
    }

    #endregion

    #region Processing Failure Tests

    [Fact]
    public async Task Handle_ShouldStillSucceed_WhenProcessingFails()
    {
        // Arrange
        var fileName = "test-data.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var fileSize = stream.Length;

        var command = new UploadFileCommand
        {
            FileName = fileName,
            FileStream = stream,
            FileSize = fileSize,
            UserId = "user123",
            ProcessImmediately = true
        };

        _mockValidationService
            .Setup(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(true));

        _mockValidationService
            .Setup(x => x.GetFileExtension(fileName))
            .Returns(".csv");

        _mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, "user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/user123/test-data.csv");

        _mockProcessingService
            .Setup(x => x.ProcessFileAsync(
                It.IsAny<string>(),
                It.IsAny<FileType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.FileProcessingResult(
                false,
                Error: "CSV file is malformed"
            ));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); // Upload succeeded even though processing failed
        result.FilePath.Should().Be("/uploads/user123/test-data.csv");
        result.ProcessingResult.Should().NotBeNull();
        result.ProcessingResult!.IsSuccess.Should().BeFalse();
        result.ProcessingResult.Error.Should().Contain("malformed");
    }

    #endregion

    #region File Type Tests

    [Theory]
    [InlineData("data.csv", ".csv")]
    [InlineData("config.json", ".json")]
    [InlineData("document.xml", ".xml")]
    [InlineData("spreadsheet.xlsx", ".xlsx")]
    [InlineData("notes.txt", ".txt")]
    public async Task Handle_ShouldSupportVariousFileTypes(string fileName, string extension)
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var fileSize = stream.Length;

        var command = new UploadFileCommand
        {
            FileName = fileName,
            FileStream = stream,
            FileSize = fileSize,
            UserId = "user123",
            ProcessImmediately = false
        };

        _mockValidationService
            .Setup(x => x.ValidateFileAsync(fileName, fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(true));

        _mockValidationService
            .Setup(x => x.GetFileExtension(fileName))
            .Returns(extension);

        _mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), fileName, "user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync($"/uploads/user123/{fileName}");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FilePath.Should().Contain(fileName);
    }

    #endregion
}
