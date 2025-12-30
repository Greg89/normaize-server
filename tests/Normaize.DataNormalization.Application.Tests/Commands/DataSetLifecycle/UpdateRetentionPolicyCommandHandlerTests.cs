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

public class UpdateRetentionPolicyCommandHandlerTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<UpdateRetentionPolicyCommandHandler>> _mockLogger;
    private readonly UpdateRetentionPolicyCommandHandler _handler;

    public UpdateRetentionPolicyCommandHandlerTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<UpdateRetentionPolicyCommandHandler>>();

        _handler = new UpdateRetentionPolicyCommandHandler(
            _mockRepository.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidInputs_ShouldUpdateRetentionPolicySuccessfully()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var retentionDays = 30;

        var dataSet = DataSet.Create(
            "Test Dataset",
            "Test Description",
            userId,
            new FileMetadata("test.csv", "uploads/test.csv", Domain.ValueObjects.FileType.CSV, StorageProvider.Local, 1024));

        var command = new UpdateRetentionPolicyCommand
        {
            DataSetId = dataSetId,
            RetentionDays = retentionDays,
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
        result.Message.Should().Contain("updated successfully");
        result.RetentionDays.Should().Be(retentionDays);
        result.ExpiryDate.Should().NotBeNull();
        result.IsExpired.Should().BeFalse();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => ds.RetentionExpiryDate.HasValue), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(
            It.IsAny<Guid>(),
            userId,
            "UpdateRetentionPolicy",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentDataSet_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateRetentionPolicyCommand
        {
            DataSetId = Guid.NewGuid(),
            RetentionDays = 30,
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

        var command = new UpdateRetentionPolicyCommand
        {
            DataSetId = dataSetId,
            RetentionDays = 30,
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
