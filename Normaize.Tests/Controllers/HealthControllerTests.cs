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
        var response = okResult.Value;
        // Check properties
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("timestamp").Should().NotBeNull();
        responseType.GetProperty("service").Should().NotBeNull();
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
        var response = okResult.Value;
        var responseType = response.GetType();
        var statusProperty = responseType.GetProperty("status");
        statusProperty.Should().NotBeNull();
        var statusValue = statusProperty!.GetValue(response);
        statusValue.Should().Be("healthy");
        var timestampProperty = responseType.GetProperty("timestamp");
        timestampProperty.Should().NotBeNull();
        var timestampValue = timestampProperty!.GetValue(response);
        timestampValue.Should().NotBeNull();
        var serviceProperty = responseType.GetProperty("service");
        serviceProperty.Should().NotBeNull();
        var serviceValue = serviceProperty!.GetValue(response);
        serviceValue.Should().Be("Normaize API");
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
        var response = okResult.Value;
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("timestamp").Should().NotBeNull();
        responseType.GetProperty("service").Should().NotBeNull();
        responseType.GetProperty("version").Should().NotBeNull();
        responseType.GetProperty("environment").Should().NotBeNull();
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
        var response = okResult.Value;
        var responseType = response.GetType();
        var statusProperty = responseType.GetProperty("status");
        statusProperty.Should().NotBeNull();
        var statusValue = statusProperty!.GetValue(response);
        statusValue.Should().Be("healthy");
        var timestampProperty = responseType.GetProperty("timestamp");
        timestampProperty.Should().NotBeNull();
        var timestampValue = timestampProperty!.GetValue(response);
        timestampValue.Should().NotBeNull();
        var serviceProperty = responseType.GetProperty("service");
        serviceProperty.Should().NotBeNull();
        var serviceValue = serviceProperty!.GetValue(response);
        serviceValue.Should().Be("Normaize API");
        var versionProperty = responseType.GetProperty("version");
        versionProperty.Should().NotBeNull();
        var versionValue = versionProperty!.GetValue(response);
        versionValue.Should().Be("1.0.0");
        var environmentProperty = responseType.GetProperty("environment");
        environmentProperty.Should().NotBeNull();
        var environmentValue = environmentProperty!.GetValue(response);
        environmentValue.Should().NotBeNull();
        environmentValue!.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void GetBasic_ShouldIncludeEnvironmentInformation()
    {
        // Act
        var result = _controller.GetBasic();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value;
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
        var response = okResult.Value;
        var responseType = response.GetType();
        var versionProperty = responseType.GetProperty("version");
        versionProperty.Should().NotBeNull();
        var versionValue = versionProperty!.GetValue(response);
        versionValue.Should().NotBeNull();
        versionValue!.ToString().Should().Be("1.0.0");
    }
} 