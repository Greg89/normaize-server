using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Commands.DataSets;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Tests.Commands;

public class UpdateDataSetCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateRetentionExpiryDate_WhenProvided()
    {
        // Arrange
        var userId = "user123";
        var dataSet = CreateDataSet(userId);
        var newExpiry = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(10).Date, DateTimeKind.Utc);

        var dataSetRepository = new Mock<IDataSetRepository>();
        var auditService = new Mock<IAuditService>();
        var logger = new Mock<ILogger<UpdateDataSetCommandHandler>>();

        dataSetRepository
            .Setup(r => r.GetByIdAsync(dataSet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        DataSet? updatedEntity = null;
        dataSetRepository
            .Setup(r => r.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataSet ds, CancellationToken _) =>
            {
                updatedEntity = ds;
                return ds;
            });

        auditService
            .Setup(a => a.LogDataSetActionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateDataSetCommandHandler(dataSetRepository.Object, auditService.Object, logger.Object);

        var command = new UpdateDataSetCommand(
            dataSet.Id,
            userId,
            "WOI Dealer Car Search",
            "test change",
            newExpiry,
            userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        updatedEntity.Should().NotBeNull();
        updatedEntity!.RetentionPolicy.Should().NotBeNull();
        updatedEntity.RetentionPolicy!.ExpiryDate.Should().Be(newExpiry);

        dataSetRepository.Verify(r => r.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotChangeRetentionExpiryDate_WhenNotProvided()
    {
        // Arrange
        var userId = "user123";
        var dataSet = CreateDataSet(userId);
        var existingExpiry = dataSet.RetentionPolicy!.ExpiryDate;

        var dataSetRepository = new Mock<IDataSetRepository>();
        var auditService = new Mock<IAuditService>();
        var logger = new Mock<ILogger<UpdateDataSetCommandHandler>>();

        dataSetRepository
            .Setup(r => r.GetByIdAsync(dataSet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSet);

        DataSet? updatedEntity = null;
        dataSetRepository
            .Setup(r => r.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataSet ds, CancellationToken _) =>
            {
                updatedEntity = ds;
                return ds;
            });

        auditService
            .Setup(a => a.LogDataSetActionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateDataSetCommandHandler(dataSetRepository.Object, auditService.Object, logger.Object);

        var command = new UpdateDataSetCommand(
            dataSet.Id,
            userId,
            "WOI Dealer Car Search",
            "test change",
            null,
            userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        updatedEntity.Should().NotBeNull();
        updatedEntity!.RetentionPolicy.Should().NotBeNull();
        updatedEntity.RetentionPolicy!.ExpiryDate.Should().Be(existingExpiry);

        dataSetRepository.Verify(r => r.UpdateAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DataSet CreateDataSet(string userId)
    {
        var fileInfo = FileMetadata.CreateFromFileName(
            "test.csv",
            "/uploads/user123/test.csv",
            123);

        return DataSet.Create(
            "Initial Name",
            "Initial Description",
            userId,
            fileInfo,
            statistics: DatasetStatistics.Empty,
            retentionDays: 365);
    }
}
