using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _controller = new HealthController(_mockLoggingService.Object);
    }

    [Fact]
    public void Get_ShouldReturnOkResultWithHealthStatus()
    {
        // Act
        var result = _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>().Subject.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value!;
        // Check properties
        var responseType = response.GetType();
        responseType.GetProperty("Status").Should().NotBeNull();
        responseType.GetProperty("Timestamp").Should().NotBeNull();
        responseType.GetProperty("Service").Should().NotBeNull();
        responseType.GetProperty("Version").Should().NotBeNull();
        responseType.GetProperty("Environment").Should().NotBeNull();
        // Verify logging was called
        _mockLoggingService.Verify(
            x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object?>()),
            Times.Once);
    }

    [Fact]
    public void Get_ShouldReturnCorrectHealthData()
    {
        // Act
        var result = _controller.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value!;
        var responseType = response.GetType();
        var statusProperty = responseType.GetProperty("Status");
        statusProperty.Should().NotBeNull();
        var statusValue = statusProperty!.GetValue(response);
        statusValue.Should().Be("healthy");
        var timestampProperty = responseType.GetProperty("Timestamp");
        timestampProperty.Should().NotBeNull();
        var timestampValue = timestampProperty!.GetValue(response);
        timestampValue.Should().NotBeNull();
        var serviceProperty = responseType.GetProperty("Service");
        serviceProperty.Should().NotBeNull();
        var serviceValue = serviceProperty!.GetValue(response);
        serviceValue.Should().Be("Normaize API");
        var versionProperty = responseType.GetProperty("Version");
        versionProperty.Should().NotBeNull();
        var versionValue = versionProperty!.GetValue(response);
        versionValue.Should().Be("1.0.0");
    }

    [Fact]
    public void Get_ShouldIncludeEnvironmentInformation()
    {
        // Act
        var result = _controller.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value!;
        var responseType = response.GetType();
        var environmentProperty = responseType.GetProperty("Environment");
        environmentProperty.Should().NotBeNull();
        var environmentValue = environmentProperty!.GetValue(response);
        environmentValue.Should().NotBeNull();
        environmentValue!.ToString().Should().NotBeEmpty();
    }
} 