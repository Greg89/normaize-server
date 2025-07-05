using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.API.Services;
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
        var response = okResult.Value.Should().BeOfType<dynamic>().Subject!;
        
        // Verify logging was called
        _mockLoggingService.Verify(
            x => x.LogUserAction("Health check requested", null),
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
        var response = okResult.Value as dynamic;
        
        // Use reflection to check properties since we're using dynamic
        var responseType = response.GetType();
        var statusProperty = responseType.GetProperty("status");
        statusProperty.Should().NotBeNull();
        var timestampProperty = responseType.GetProperty("timestamp");
        timestampProperty.Should().NotBeNull();
        var serviceProperty = responseType.GetProperty("service");
        serviceProperty.Should().NotBeNull();
    }

    [Fact]
    public void GetBasic_ShouldReturnOkResultWithDetailedHealthStatus()
    {
        // Act
        var result = _controller.GetBasic();

        // Assert
        result.Should().BeOfType<OkObjectResult>().Subject.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value.Should().BeOfType<dynamic>().Subject!;
        
        // Verify logging was called
        _mockLoggingService.Verify(
            x => x.LogUserAction("Basic health check requested", null),
            Times.Once);
    }

    [Fact]
    public void GetBasic_ShouldReturnCorrectDetailedHealthData()
    {
        // Act
        var result = _controller.GetBasic();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value as dynamic;
        
        // Use reflection to check properties
        var responseType = response.GetType();
        var statusProperty = responseType.GetProperty("status");
        statusProperty.Should().NotBeNull();
        var timestampProperty = responseType.GetProperty("timestamp");
        timestampProperty.Should().NotBeNull();
        var serviceProperty = responseType.GetProperty("service");
        serviceProperty.Should().NotBeNull();
        var versionProperty = responseType.GetProperty("version");
        versionProperty.Should().NotBeNull();
        var environmentProperty = responseType.GetProperty("environment");
        environmentProperty.Should().NotBeNull();
    }

    [Fact]
    public void GetBasic_ShouldIncludeEnvironmentInformation()
    {
        // Act
        var result = _controller.GetBasic();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value as dynamic;
        
        // The environment should be set (either from environment variable or default)
        var responseType = response.GetType();
        var environmentProperty = responseType.GetProperty("environment");
        environmentProperty.Should().NotBeNull();
        var environmentValue = environmentProperty!.GetValue(response);
        environmentValue.Should().NotBeNull();
        environmentValue!.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void GetBasic_ShouldIncludeVersionInformation()
    {
        // Act
        var result = _controller.GetBasic();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value as dynamic;
        
        // The version should be set
        var responseType = response.GetType();
        var versionProperty = responseType.GetProperty("version");
        versionProperty.Should().NotBeNull();
        var versionValue = versionProperty!.GetValue(response);
        versionValue.Should().NotBeNull();
        versionValue!.ToString().Should().Be("1.0.0");
    }
} 