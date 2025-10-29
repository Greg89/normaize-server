using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary;
using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Commands.Statistics;

/// <summary>
/// Unit tests for GenerateDataSummaryCommandHandler
/// </summary>
public class GenerateDataSummaryCommandHandlerTests
{
    private readonly Mock<IDataSetRepository> _mockDataSetRepository;
    private readonly Mock<IStatisticsRepository> _mockStatisticsRepository;
    private readonly Mock<IStatisticalCalculationService> _mockCalculationService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<GenerateDataSummaryCommandHandler>> _mockLogger;
    private readonly GenerateDataSummaryCommandHandler _handler;

    public GenerateDataSummaryCommandHandlerTests()
    {
        _mockDataSetRepository = new Mock<IDataSetRepository>();
        _mockStatisticsRepository = new Mock<IStatisticsRepository>();
        _mockCalculationService = new Mock<IStatisticalCalculationService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<GenerateDataSummaryCommandHandler>>();

        _handler = new GenerateDataSummaryCommandHandler(
            _mockDataSetRepository.Object,
            _mockStatisticsRepository.Object,
            _mockCalculationService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyDataSummary_WhenDataSetNotFound()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var command = new GenerateDataSummaryCommand(dataSetId, userId);
        var cancellationToken = CancellationToken.None;

        _mockDataSetRepository.Setup(x => x.GetByIdAsync(dataSetId, cancellationToken))
            .ReturnsAsync((DataSet?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(command, cancellationToken));

        exception.Message.Should().Contain($"DataSet with ID {dataSetId} not found");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenUserDoesNotOwnDataSet()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var ownerId = "different-user";
        var command = new GenerateDataSummaryCommand(dataSetId, userId);
        var cancellationToken = CancellationToken.None;

        var dataSet = CreateTestDataSet(dataSetId, ownerId);

        _mockDataSetRepository.Setup(x => x.GetByIdAsync(dataSetId, cancellationToken))
            .ReturnsAsync(dataSet);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, cancellationToken));

        exception.Message.Should().Contain("User does not have access to this dataset");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyDataSummary_WhenDataSetHasNoData()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var command = new GenerateDataSummaryCommand(dataSetId, userId);
        var cancellationToken = CancellationToken.None;

        var dataSet = CreateTestDataSet(dataSetId, userId);
        var expectedDto = CreateEmptyDataSummaryDto(dataSetId);

        _mockDataSetRepository.Setup(x => x.GetByIdAsync(dataSetId, cancellationToken))
            .ReturnsAsync(dataSet);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(0);
        result.TotalColumns.Should().Be(0);
        result.QualityScore.OverallScore.Should().Be(100);

        // Should not call calculation service when there's no data
        _mockCalculationService.Verify(x => x.GenerateDataSummaryAsync(
            It.IsAny<DataSet>(),
            It.IsAny<List<Dictionary<string, object?>>>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "test-user";
        var command = new GenerateDataSummaryCommand(dataSetId, userId);
        var cancellationToken = CancellationToken.None;

        var expectedException = new InvalidOperationException("Test exception");

        _mockDataSetRepository.Setup(x => x.GetByIdAsync(dataSetId, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, cancellationToken));

        exception.Should().Be(expectedException);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error generating data summary")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static DataSet CreateTestDataSet(Guid dataSetId, string userId)
    {
        var fileInfo = FileMetadata.Create(
            "test.csv",
            $"{userId}/test.csv",
            FileType.CSV,
            1024);

        var dataSet = DataSet.Create(
            name: "Test Dataset",
            description: "Test description",
            userId: userId,
            fileInfo: fileInfo,
            statistics: null,
            retentionDays: 30);

        // Use reflection to set the Id since it's only set internally
        var idProperty = typeof(DataSet).GetProperty("Id");
        idProperty?.SetValue(dataSet, dataSetId);

        return dataSet;
    }

    private static DataSummaryDto CreateEmptyDataSummaryDto(Guid dataSetId)
    {
        return new DataSummaryDto
        {
            DataSetId = (int)dataSetId.GetHashCode(),
            TotalRows = 0,
            TotalColumns = 0,
            MissingValues = 0,
            DuplicateRows = 0,
            ColumnSummaries = new Dictionary<string, BasicColumnSummaryDto>(),
            GeneratedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.Zero,
            QualityScore = new DataQualityScoreDto
            {
                OverallScore = 100,
                HasQualityIssues = false,
                HasSeriousIssues = false
            }
        };
    }
}