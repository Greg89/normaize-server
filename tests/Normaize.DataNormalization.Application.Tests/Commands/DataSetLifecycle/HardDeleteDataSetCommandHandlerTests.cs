using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Commands.DataSetLifecycle;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Commands.DataSetLifecycle;

public class HardDeleteDataSetCommandHandlerTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<HardDeleteDataSetCommandHandler>> _mockLogger;
    private readonly HardDeleteDataSetCommandHandler _handler;

    public HardDeleteDataSetCommandHandlerTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<HardDeleteDataSetCommandHandler>>();

        _handler = new HardDeleteDataSetCommandHandler(
            _mockRepository.Object,
            _mockFileStorageService.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidInputs_ShouldDeleteSuccessfully()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var filePath = "uploads/test.csv";

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            userId,
            new FileMetadata("test.csv", filePath, Domain.ValueObjects.FileType.CSV, StorageProvider.Local, 1024));

        var command = new HardDeleteDataSetCommand
        {
            DataSetId = dataSetId,
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        _mockFileStorageService.Setup(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditService.Setup(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("permanently deleted");
        result.FileDeleted.Should().BeTrue();

        _mockFileStorageService.Verify(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            userId,
            "HardDeleteDataSet",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFileDeletionFailure_ShouldContinueWithDatabaseDeletion()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var filePath = "uploads/test.csv";

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            userId,
            new FileMetadata("test.csv", filePath, Domain.ValueObjects.FileType.CSV, StorageProvider.Local, 1024));

        var command = new HardDeleteDataSetCommand
        {
            DataSetId = dataSetId,
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        _mockFileStorageService.Setup(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("File deletion failed"));

        _mockRepository.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditService.Setup(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FileDeleted.Should().BeFalse();

        _mockFileStorageService.Verify(x => x.DeleteFileAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentDataSet_ShouldReturnNotFound()
    {
        // Arrange
        var command = new HardDeleteDataSetCommand
        {
            DataSetId = Guid.NewGuid(),
            UserId = "test-user"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataSet?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithUnauthorizedUser_ShouldReturnAccessDenied()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var ownerId = "owner-user";
        var unauthorizedUserId = "unauthorized-user";

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            ownerId,
            new FileMetadata("test.csv", "uploads/test.csv", Domain.ValueObjects.FileType.CSV, StorageProvider.Local, 1024));

        var command = new HardDeleteDataSetCommand
        {
            DataSetId = dataSetId,
            UserId = unauthorizedUserId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }
}
