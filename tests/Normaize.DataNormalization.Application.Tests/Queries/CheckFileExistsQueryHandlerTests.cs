using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Queries.FileUpload;

namespace Normaize.DataNormalization.Application.Tests.Queries;

public class CheckFileExistsQueryHandlerTests
{
    private readonly Mock<IFileStorageService> _mockStorageService;
    private readonly Mock<ILogger<CheckFileExistsQueryHandler>> _mockLogger;
    private readonly CheckFileExistsQueryHandler _handler;

    public CheckFileExistsQueryHandlerTests()
    {
        _mockStorageService = new Mock<IFileStorageService>();
        _mockLogger = new Mock<ILogger<CheckFileExistsQueryHandler>>();

        _handler = new CheckFileExistsQueryHandler(
            _mockStorageService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        var filePath = "/uploads/user123/test-data.csv";
        var query = new CheckFileExistsQuery
        {
            FilePath = filePath
        };

        var stream = new MemoryStream();
        _mockStorageService
            .Setup(x => x.GetFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Exists.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenFileNotFound()
    {
        // Arrange
        var filePath = "/uploads/user123/nonexistent.csv";
        var query = new CheckFileExistsQuery
        {
            FilePath = filePath
        };

        _mockStorageService
            .Setup(x => x.GetFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Exists.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalseWithError_WhenStorageServiceFails()
    {
        // Arrange
        var filePath = "/uploads/user123/test-data.csv";
        var query = new CheckFileExistsQuery
        {
            FilePath = filePath
        };

        _mockStorageService
            .Setup(x => x.GetFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Network error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Exists.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }
}
