using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Commands.FileUpload;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Application.Tests.Commands;

public class DeleteFileCommandHandlerTests
{
    private readonly Mock<IFileStorageService> _mockStorageService;
    private readonly Mock<ILogger<DeleteFileCommandHandler>> _mockLogger;
    private readonly DeleteFileCommandHandler _handler;

    public DeleteFileCommandHandlerTests()
    {
        _mockStorageService = new Mock<IFileStorageService>();
        _mockLogger = new Mock<ILogger<DeleteFileCommandHandler>>();

        _handler = new DeleteFileCommandHandler(
            _mockStorageService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldDeleteFile_WhenFileExists()
    {
        // Arrange
        var filePath = "/uploads/user123/test-data.csv";
        var command = new DeleteFileCommand
        {
            FilePath = filePath,
            UserId = "user123"
        };

        _mockStorageService
            .Setup(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockStorageService.Verify(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenFileNotFound()
    {
        // Arrange
        var filePath = "/uploads/user123/nonexistent.csv";
        var command = new DeleteFileCommand
        {
            FilePath = filePath,
            UserId = "user123"
        };

        _mockStorageService
            .Setup(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); // Treat missing file as success (idempotent)
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenStorageServiceFails()
    {
        // Arrange
        var filePath = "/uploads/user123/test-data.csv";
        var command = new DeleteFileCommand
        {
            FilePath = filePath,
            UserId = "user123"
        };

        _mockStorageService
            .Setup(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Permission denied"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Delete failed");
        result.Error.Should().Contain("Permission denied");
    }
}
