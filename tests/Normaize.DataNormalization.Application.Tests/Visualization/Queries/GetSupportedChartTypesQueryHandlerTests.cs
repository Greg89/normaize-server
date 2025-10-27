using FluentAssertions;
using Normaize.DataNormalization.Application.Visualization.Queries.GetSupportedChartTypes;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Visualization.Queries;

public class GetSupportedChartTypesQueryHandlerTests
{
    private readonly GetSupportedChartTypesQueryHandler _handler;

    public GetSupportedChartTypesQueryHandlerTests()
    {
        _handler = new GetSupportedChartTypesQueryHandler();
    }

    [Fact]
    public async Task Handle_ReturnsAllChartTypes()
    {
        // Arrange
        var query = new GetSupportedChartTypesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().Contain(ChartType.Bar);
        result.Should().Contain(ChartType.Line);
        result.Should().Contain(ChartType.Pie);
        result.Should().Contain(ChartType.Scatter);
        result.Should().Contain(ChartType.Area);
        result.Should().Contain(ChartType.Histogram);
        result.Should().Contain(ChartType.BoxPlot);
        result.Should().Contain(ChartType.Heatmap);
        result.Should().Contain(ChartType.Bubble);
        result.Should().Contain(ChartType.Radar);
        result.Should().Contain(ChartType.Donut);
        result.Should().Contain(ChartType.Column);
    }

    [Fact]
    public async Task Handle_Returns12ChartTypes()
    {
        // Arrange
        var query = new GetSupportedChartTypesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(12);
    }
}
