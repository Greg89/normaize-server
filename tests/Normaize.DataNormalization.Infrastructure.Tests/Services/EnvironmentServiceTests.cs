using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

public class EnvironmentServiceTests
{
    private readonly Mock<IHostEnvironment> _mockHostEnvironment;
    private readonly Mock<ILogger<EnvironmentService>> _mockLogger;
    private readonly EnvironmentService _service;

    public EnvironmentServiceTests()
    {
        _mockHostEnvironment = new Mock<IHostEnvironment>();
        _mockLogger = new Mock<ILogger<EnvironmentService>>();
        _service = new EnvironmentService(_mockHostEnvironment.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    [InlineData("Beta", true)]
    [InlineData("production", true)] // Case insensitive
    [InlineData("PRODUCTION", true)]
    [InlineData("Development", false)]
    [InlineData("Test", false)]
    [InlineData("Local", false)]
    public void IsProductionLike_ShouldReturnCorrectResult_ForEnvironment(string environmentName, bool expected)
    {
        // Arrange
        _mockHostEnvironment.Setup(x => x.EnvironmentName).Returns(environmentName);

        // Act
        var result = _service.IsProductionLike();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsContainerized_ShouldReturnTrue_WhenPortEnvironmentVariableExists()
    {
        // Arrange
        var originalPort = Environment.GetEnvironmentVariable("PORT");
        try
        {
            Environment.SetEnvironmentVariable("PORT", "5000");

            // Act
            var result = _service.IsContainerized();

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORT", originalPort);
        }
    }

    [Fact]
    public void IsContainerized_ShouldReturnTrue_WhenDotNetContainerVariableIsTrue()
    {
        // Arrange
        var originalContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

            // Act
            var result = _service.IsContainerized();

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", originalContainer);
        }
    }

    [Fact]
    public void IsContainerized_ShouldReturnFalse_WhenNoContainerIndicatorsPresent()
    {
        // Arrange
        var originalPort = Environment.GetEnvironmentVariable("PORT");
        var originalContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        try
        {
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);

            // Act
            var result = _service.IsContainerized();

            // Assert
            // Note: May return true if /.dockerenv exists, but in normal test environment should be false
            result.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORT", originalPort);
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", originalContainer);
        }
    }

    [Fact]
    public void GetEnvironmentName_ShouldReturnHostEnvironmentName()
    {
        // Arrange
        _mockHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        // Act
        var result = _service.GetEnvironmentName();

        // Assert
        result.Should().Be("Development");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHostEnvironmentIsNull()
    {
        // Act
        var act = () => new EnvironmentService(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("hostEnvironment");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new EnvironmentService(_mockHostEnvironment.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
