using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.API.Tests.Controllers;

public class DataSetsControllerTests
{
    private readonly Mock<IDataSetRepository> _repositoryMock;
    private readonly Mock<IDataSetDataLoader> _dataLoaderMock;
    private readonly Mock<ILogger<DataSetsController>> _loggerMock;
    private readonly DataSetsController _controller;

    public DataSetsControllerTests()
    {
        _repositoryMock = new Mock<IDataSetRepository>();
        _dataLoaderMock = new Mock<IDataSetDataLoader>();
        _loggerMock = new Mock<ILogger<DataSetsController>>();
        _controller = new DataSetsController(_repositoryMock.Object, _dataLoaderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Controller_ShouldBeProperlyConfigured()
    {
        // Assert
        _controller.Should().NotBeNull();
        _controller.Should().BeOfType<DataSetsController>();
    }
}