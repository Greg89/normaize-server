using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.API.Tests.Controllers;

public class DataSetsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IDataSetDataLoader> _dataLoaderMock;
    private readonly Mock<ILogger<DataSetsController>> _loggerMock;
    private readonly DataSetsController _controller;

    public DataSetsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _dataLoaderMock = new Mock<IDataSetDataLoader>();
        _loggerMock = new Mock<ILogger<DataSetsController>>();
        _controller = new DataSetsController(_mediatorMock.Object, _dataLoaderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Controller_ShouldBeProperlyConfigured()
    {
        // Assert
        _controller.Should().NotBeNull();
        _controller.Should().BeOfType<DataSetsController>();
    }
}