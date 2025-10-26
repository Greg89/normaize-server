using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary;
using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Commands.Statistics;

/// <summary>
/// Unit tests for GenerateDataSummaryCommandHandler
/// </summary>
public class GenerateDataSummaryCommandHandlerTests
{
    private readonly Mock<IStatisticalCalculationService> _mockStatisticalService;
    private readonly Mock<IStatisticsRepository> _mockStatisticsRepository;
    private readonly Mock<IStatisticsMapper> _mockMapper;
    private readonly Mock<ILogger<GenerateDataSummaryCommandHandler>> _mockLogger;
    private readonly GenerateDataSummaryCommandHandler _handler;

    public GenerateDataSummaryCommandHandlerTests()
    {
        _mockStatisticalService = new Mock<IStatisticalCalculationService>();
        _mockStatisticsRepository = new Mock<IStatisticsRepository>();
        _mockMapper = new Mock<IStatisticsMapper>();
        _mockLogger = new Mock<ILogger<GenerateDataSummaryCommandHandler>>();

        _handler = new GenerateDataSummaryCommandHandler(
            _mockStatisticalService.Object,
            _mockStatisticsRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldGenerateDataSummary_WhenValidCommand()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var command = new GenerateDataSummaryCommand(dataSetId);
        var cancellationToken = CancellationToken.None;

        var statistics = CreateTestStatistics(dataSetId);
        var expectedDto = CreateTestDataSummaryDto();

        _mockStatisticalService.Setup(x => x.GenerateDataSummaryAsync(dataSetId, cancellationToken))
            .ReturnsAsync(statistics);

        _mockStatisticsRepository.Setup(x => x.AddAsync(It.IsAny<Domain.Aggregates.Statistics>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(x => x.MapToDataSummaryDto(statistics, It.IsAny<TimeSpan>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);

        _mockStatisticalService.Verify(x => x.GenerateDataSummaryAsync(dataSetId, cancellationToken), Times.Once);
        _mockStatisticsRepository.Verify(x => x.AddAsync(It.Is<Domain.Aggregates.Statistics>(s => s.DataSetId == dataSetId), cancellationToken), Times.Once);
        _mockMapper.Verify(x => x.MapToDataSummaryDto(statistics, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenDataSetIdIsEmpty()
    {
        // Arrange
        var command = new GenerateDataSummaryCommand(Guid.Empty);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _handler.Handle(command, cancellationToken));

        exception.Message.Should().Contain("DataSetId cannot be empty");
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingStatistics_WhenStatisticsAlreadyExist()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var command = new GenerateDataSummaryCommand(dataSetId);
        var cancellationToken = CancellationToken.None;

        var existingStatistics = CreateTestStatistics(dataSetId);
        var newStatistics = CreateTestStatistics(dataSetId);
        var expectedDto = CreateTestDataSummaryDto();

        _mockStatisticsRepository.Setup(x => x.GetByDataSetIdAsync(dataSetId, cancellationToken))
            .ReturnsAsync(existingStatistics);

        _mockStatisticalService.Setup(x => x.GenerateDataSummaryAsync(dataSetId, cancellationToken))
            .ReturnsAsync(newStatistics);

        _mockStatisticsRepository.Setup(x => x.UpdateAsync(It.IsAny<Domain.Aggregates.Statistics>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(x => x.MapToDataSummaryDto(It.IsAny<Domain.Aggregates.Statistics>(), It.IsAny<TimeSpan>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        _mockStatisticsRepository.Verify(x => x.UpdateAsync(It.IsAny<Domain.Aggregates.Statistics>(), cancellationToken), Times.Once);
        _mockStatisticsRepository.Verify(x => x.AddAsync(It.IsAny<Domain.Aggregates.Statistics>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var command = new GenerateDataSummaryCommand(dataSetId);
        var cancellationToken = CancellationToken.None;

        var expectedException = new InvalidOperationException("Test exception");

        _mockStatisticalService.Setup(x => x.GenerateDataSummaryAsync(dataSetId, cancellationToken))
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

    private static Domain.Aggregates.Statistics CreateTestStatistics(Guid dataSetId)
    {
        var columnSummaries = new Dictionary<string, ColumnSummary>
        {
            ["name"] = new ColumnSummary(
                "name",
                new DataTypeClassification("String", false, false, false),
                100,
                0,
                100,
                new List<object> { "John", "Jane", "Bob" },
                "Alice",
                "Zoe",
                null),
            ["age"] = new ColumnSummary(
                "age",
                new DataTypeClassification("Numeric", true, false, false),
                98,
                2,
                50,
                new List<object> { 25, 30, 35 },
                18,
                75,
                new StatisticalMeasure(42.5, 40, 15.2, 18, 75, 30, 40, 55, 0.2, -0.5, 3))
        };

        return Domain.Aggregates.Statistics.CreateDataSummary(
            dataSetId,
            "Test Dataset",
            100,
            2,
            columnSummaries,
            TimeSpan.FromSeconds(5));
    }

    private static DataSummaryDto CreateTestDataSummaryDto()
    {
        return new DataSummaryDto
        {
            DataSetId = 1,
            TotalRows = 100,
            TotalColumns = 2,
            MissingValues = 2,
            DuplicateRows = 0,
            ColumnSummaries = new Dictionary<string, Application.Common.DTOs.ColumnSummaryDto>(),
            GeneratedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromSeconds(5),
            QualityScore = new DataQualityScoreDto()
        };
    }
}