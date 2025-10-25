using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.Application.Commands;

namespace Normaize.DataNormalization.API.Tests.Controllers;

public class DataNormalizationControllerTests
{
    private readonly Mock<ICommandHandler<SubmitJobCommand, object>> _submitJobHandlerMock;
    private readonly Mock<ILogger<DataNormalizationController>> _loggerMock;
    private readonly DataNormalizationController _controller;

    public DataNormalizationControllerTests()
    {
        _submitJobHandlerMock = new Mock<ICommandHandler<SubmitJobCommand, object>>();
        _loggerMock = new Mock<ILogger<DataNormalizationController>>();
        _controller = new DataNormalizationController(_submitJobHandlerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Controller_ShouldBeProperlyConfigured()
    {
        // Assert
        _controller.Should().NotBeNull();
        _controller.Should().BeOfType<DataNormalizationController>();
    }
}