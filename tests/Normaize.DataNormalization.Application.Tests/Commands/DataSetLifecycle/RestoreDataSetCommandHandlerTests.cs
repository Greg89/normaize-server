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

public class RestoreDataSetCommandHandlerTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<RestoreDataSetCommandHandler>> _mockLogger;
    private readonly RestoreDataSetCommandHandler _handler;

    public RestoreDataSetCommandHandlerTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<RestoreDataSetCommandHandler>>();

        _handler = new RestoreDataSetCommandHandler(
            _mockRepository.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithDeletedDataSet_ShouldRestoreSuccessfully()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            userId,
            new FileMetadata("test.csv", "s3://normaize-uploads/test.csv", Domain.ValueObjects.FileType.CSV, StorageProvider.S3, 1024));

        // Soft delete the dataset
        dataSet.Delete(userId);

        var command = new RestoreDataSetCommand
        {
            DataSetId = dataSetId,
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

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => !ds.IsDeleted), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            userId,
            "RestoreDataSet",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonDeletedDataSet_ShouldReturnSuccessWithoutAction()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            userId,
            new FileMetadata("test.csv", "s3://normaize-uploads/test.csv", Domain.ValueObjects.FileType.CSV, StorageProvider.S3, 1024));

        var command = new RestoreDataSetCommand
        {
            DataSetId = dataSetId,
            UserId = userId
        };

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("not deleted");

        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentDataSet_ShouldReturnNotFound()
    {
        // Arrange
        var command = new RestoreDataSetCommand
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
            new FileMetadata("test.csv", "s3://normaize-uploads/test.csv", Domain.ValueObjects.FileType.CSV, StorageProvider.S3, 1024));

        var command = new RestoreDataSetCommand
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
