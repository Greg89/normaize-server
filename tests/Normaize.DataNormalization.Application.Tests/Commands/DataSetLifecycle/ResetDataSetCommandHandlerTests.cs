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

public class ResetDataSetCommandHandlerTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IFileProcessingService> _mockFileProcessingService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<ResetDataSetCommandHandler>> _mockLogger;
    private readonly ResetDataSetCommandHandler _handler;

    public ResetDataSetCommandHandlerTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockFileProcessingService = new Mock<IFileProcessingService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<ResetDataSetCommandHandler>>();

        _handler = new ResetDataSetCommandHandler(
            _mockRepository.Object,
            _mockFileStorageService.Object,
            _mockFileProcessingService.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ReprocessReset_WithValidFile_ShouldReprocessSuccessfully()
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

        var command = new ResetDataSetCommand
        {
            DataSetId = dataSetId,
            ResetType = ResetType.Reprocess,
            Reason = "Testing reset",
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        _mockFileStorageService.Setup(x => x.FileExistsAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileProcessingService.Setup(x => x.ProcessFileAsync(filePath, Domain.ValueObjects.FileType.CSV, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(true, "schema", 100, 5, "preview"));

        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

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
        result.Message.Should().Contain("reset successfully");
        result.ResetType.Should().Be("FileBased");
        result.FileAvailable.Should().BeTrue();
        result.Reprocessed.Should().BeTrue();

        _mockFileStorageService.Verify(x => x.FileExistsAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileProcessingService.Verify(x => x.ProcessFileAsync(filePath, Domain.ValueObjects.FileType.CSV, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => ds.IsProcessed), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            userId,
            "ResetDataSet_FileBased",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReprocessReset_WithMissingFile_ShouldReturnFileNotFound()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var filePath = "uploads/missing.csv";

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            userId,
            new FileMetadata("missing.csv", filePath, Domain.ValueObjects.FileType.CSV, StorageProvider.Local, 1024));

        var command = new ResetDataSetCommand
        {
            DataSetId = dataSetId,
            ResetType = ResetType.Reprocess,
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        _mockFileStorageService.Setup(x => x.FileExistsAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot reset dataset");
        result.FileAvailable.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_NOT_FOUND");

        _mockFileStorageService.Verify(x => x.FileExistsAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileProcessingService.Verify(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<Domain.ValueObjects.FileType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReprocessReset_WithProcessingFailure_ShouldReturnError()
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

        var command = new ResetDataSetCommand
        {
            DataSetId = dataSetId,
            ResetType = ResetType.Reprocess,
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        _mockFileStorageService.Setup(x => x.FileExistsAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileProcessingService.Setup(x => x.ProcessFileAsync(filePath, Domain.ValueObjects.FileType.CSV, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(false, Error: "Failed to parse file"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Failed to reprocess file");
        result.FileAvailable.Should().BeTrue();
        result.Error.Should().Contain("Failed to parse file");
    }

    [Fact]
    public async Task Handle_RestoreReset_WithDeletedDataSet_ShouldRestoreSuccessfully()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            userId,
            new FileMetadata("test.csv", "uploads/test.csv", Domain.ValueObjects.FileType.CSV, StorageProvider.Local, 1024));

        // Soft delete the dataset
        dataSet.Delete(userId);

        var command = new ResetDataSetCommand
        {
            DataSetId = dataSetId,
            ResetType = ResetType.Restore,
            Reason = "Accidental deletion",
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

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
        result.Message.Should().Contain("restored successfully");
        result.ResetType.Should().Be("DatabaseOnly");

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => !ds.IsDeleted), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            userId,
            "ResetDataSet_DatabaseOnly",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentDataSet_ShouldReturnNotFound()
    {
        // Arrange
        var command = new ResetDataSetCommand
        {
            DataSetId = Guid.NewGuid(),
            ResetType = ResetType.Reprocess,
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

        var command = new ResetDataSetCommand
        {
            DataSetId = dataSetId,
            ResetType = ResetType.Reprocess,
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

    [Fact]
    public async Task Handle_ReprocessReset_WithDeletedDataSet_ShouldRestoreAndReprocess()
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

        // Soft delete the dataset
        dataSet.Delete(userId);

        var command = new ResetDataSetCommand
        {
            DataSetId = dataSetId,
            ResetType = ResetType.Reprocess,
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        _mockFileStorageService.Setup(x => x.FileExistsAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileProcessingService.Setup(x => x.ProcessFileAsync(filePath, Domain.ValueObjects.FileType.CSV, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(true, "schema", 100, 5, "preview"));

        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

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
        result.Reprocessed.Should().BeTrue();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => !ds.IsDeleted && ds.IsProcessed), It.IsAny<CancellationToken>()), Times.Once);
    }
}
