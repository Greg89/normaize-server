using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.API.Services;
using Normaize.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly Mock<IHealthCheckService> _mockHealthCheckService;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _mockHealthCheckService = new Mock<IHealthCheckService>();
        _controller = new HealthController(_mockLoggingService.Object, _mockHealthCheckService.Object);
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
        responseType.GetProperty("version").Should().NotBeNull();
        responseType.GetProperty("environment").Should().NotBeNull();
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
    }

    [Fact]
    public void Get_ShouldIncludeEnvironmentInformation()
    {
        // Act
        var result = _controller.Get();

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
    public async Task GetLiveness_WhenHealthy_ShouldReturnOkResult()
    {
        // Arrange
        var healthyResult = new HealthCheckResult
        {
            IsHealthy = true,
            Status = "alive",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(45.2),
            Components = new Dictionary<string, ComponentHealth>
            {
                ["application"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(45.2)
                }
            }
        };

        _mockHealthCheckService.Setup(x => x.CheckLivenessAsync())
            .ReturnsAsync(healthyResult);

        // Act
        var result = await _controller.GetLiveness();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        
        var response = okResult.Value;
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("timestamp").Should().NotBeNull();
        responseType.GetProperty("duration").Should().NotBeNull();
        responseType.GetProperty("message").Should().NotBeNull();
        
        var statusValue = responseType.GetProperty("status")!.GetValue(response);
        statusValue.Should().Be("alive");
        
        var messageValue = responseType.GetProperty("message")!.GetValue(response);
        messageValue.Should().Be("Application is alive");
    }

    [Fact]
    public async Task GetLiveness_WhenUnhealthy_ShouldReturnServiceUnavailable()
    {
        // Arrange
        var unhealthyResult = new HealthCheckResult
        {
            IsHealthy = false,
            Status = "not_alive",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(45.2),
            Issues = new List<string> { "Application not responding" }
        };

        _mockHealthCheckService.Setup(x => x.CheckLivenessAsync())
            .ReturnsAsync(unhealthyResult);

        // Act
        var result = await _controller.GetLiveness();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(503);
        
        var response = objectResult.Value;
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("issues").Should().NotBeNull();
        
        var statusValue = responseType.GetProperty("status")!.GetValue(response);
        statusValue.Should().Be("not_alive");
    }

    [Fact]
    public async Task GetReadiness_WhenHealthy_ShouldReturnOkResult()
    {
        // Arrange
        var healthyResult = new HealthCheckResult
        {
            IsHealthy = true,
            Status = "ready",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(190.8),
            Components = new Dictionary<string, ComponentHealth>
            {
                ["database"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(120.5)
                },
                ["application"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(45.2)
                },
                ["storage"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(25.1)
                }
            }
        };

        _mockHealthCheckService.Setup(x => x.CheckReadinessAsync())
            .ReturnsAsync(healthyResult);

        // Act
        var result = await _controller.GetReadiness();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        
        var response = okResult.Value;
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("components").Should().NotBeNull();
        responseType.GetProperty("timestamp").Should().NotBeNull();
        responseType.GetProperty("duration").Should().NotBeNull();
        responseType.GetProperty("message").Should().NotBeNull();
        
        var statusValue = responseType.GetProperty("status")!.GetValue(response);
        statusValue.Should().Be("ready");
        
        var messageValue = responseType.GetProperty("message")!.GetValue(response);
        messageValue.Should().Be("Application is ready to serve traffic");
        
        var componentsValue = responseType.GetProperty("components")!.GetValue(response);
        componentsValue.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReadiness_WhenUnhealthy_ShouldReturnServiceUnavailable()
    {
        // Arrange
        var unhealthyResult = new HealthCheckResult
        {
            IsHealthy = false,
            Status = "not_ready",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(190.8),
            Components = new Dictionary<string, ComponentHealth>
            {
                ["database"] = new ComponentHealth
                {
                    IsHealthy = false,
                    Status = "unhealthy",
                    ErrorMessage = "Cannot connect to database",
                    Duration = TimeSpan.FromMilliseconds(120.5)
                }
            },
            Issues = new List<string> { "Database: Cannot connect to database" }
        };

        _mockHealthCheckService.Setup(x => x.CheckReadinessAsync())
            .ReturnsAsync(unhealthyResult);

        // Act
        var result = await _controller.GetReadiness();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(503);
        
        var response = objectResult.Value;
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("components").Should().NotBeNull();
        responseType.GetProperty("issues").Should().NotBeNull();
        
        var statusValue = responseType.GetProperty("status")!.GetValue(response);
        statusValue.Should().Be("not_ready");
    }

    [Fact]
    public async Task GetComprehensiveHealth_WhenHealthy_ShouldReturnOkResult()
    {
        // Arrange
        var healthyResult = new HealthCheckResult
        {
            IsHealthy = true,
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(214.8),
            Components = new Dictionary<string, ComponentHealth>
            {
                ["database"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(120.5)
                },
                ["application"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(45.2)
                },
                ["storage"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(25.1)
                },
                ["external_services"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(15.3)
                },
                ["system_resources"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    Duration = TimeSpan.FromMilliseconds(8.7)
                }
            }
        };

        _mockHealthCheckService.Setup(x => x.CheckHealthAsync())
            .ReturnsAsync(healthyResult);

        // Act
        var result = await _controller.GetComprehensiveHealth();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        
        var response = okResult.Value;
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("components").Should().NotBeNull();
        responseType.GetProperty("timestamp").Should().NotBeNull();
        responseType.GetProperty("duration").Should().NotBeNull();
        responseType.GetProperty("message").Should().NotBeNull();
        
        var statusValue = responseType.GetProperty("status")!.GetValue(response);
        statusValue.Should().Be("healthy");
        
        var messageValue = responseType.GetProperty("message")!.GetValue(response);
        messageValue.Should().Be("All systems healthy");
        
        var componentsValue = responseType.GetProperty("components")!.GetValue(response);
        componentsValue.Should().NotBeNull();
    }

    [Fact]
    public async Task GetComprehensiveHealth_WhenUnhealthy_ShouldReturnServiceUnavailable()
    {
        // Arrange
        var unhealthyResult = new HealthCheckResult
        {
            IsHealthy = false,
            Status = "unhealthy",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(214.8),
            Components = new Dictionary<string, ComponentHealth>
            {
                ["database"] = new ComponentHealth
                {
                    IsHealthy = false,
                    Status = "unhealthy",
                    ErrorMessage = "Missing critical columns",
                    Duration = TimeSpan.FromMilliseconds(120.5)
                }
            },
            Issues = new List<string> { "Database: Missing critical columns" }
        };

        _mockHealthCheckService.Setup(x => x.CheckHealthAsync())
            .ReturnsAsync(unhealthyResult);

        // Act
        var result = await _controller.GetComprehensiveHealth();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(503);
        
        var response = objectResult.Value;
        var responseType = response.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("components").Should().NotBeNull();
        responseType.GetProperty("issues").Should().NotBeNull();
        
        var statusValue = responseType.GetProperty("status")!.GetValue(response);
        statusValue.Should().Be("unhealthy");
    }

    [Fact]
    public async Task GetLiveness_ShouldCallHealthCheckService()
    {
        // Arrange
        var healthyResult = new HealthCheckResult { IsHealthy = true, Status = "alive" };
        _mockHealthCheckService.Setup(x => x.CheckLivenessAsync())
            .ReturnsAsync(healthyResult);

        // Act
        await _controller.GetLiveness();

        // Assert
        _mockHealthCheckService.Verify(x => x.CheckLivenessAsync(), Times.Once);
    }

    [Fact]
    public async Task GetReadiness_ShouldCallHealthCheckService()
    {
        // Arrange
        var healthyResult = new HealthCheckResult { IsHealthy = true, Status = "ready" };
        _mockHealthCheckService.Setup(x => x.CheckReadinessAsync())
            .ReturnsAsync(healthyResult);

        // Act
        await _controller.GetReadiness();

        // Assert
        _mockHealthCheckService.Verify(x => x.CheckReadinessAsync(), Times.Once);
    }

    [Fact]
    public async Task GetComprehensiveHealth_ShouldCallHealthCheckService()
    {
        // Arrange
        var healthyResult = new HealthCheckResult { IsHealthy = true, Status = "healthy" };
        _mockHealthCheckService.Setup(x => x.CheckHealthAsync())
            .ReturnsAsync(healthyResult);

        // Act
        await _controller.GetComprehensiveHealth();

        // Assert
        _mockHealthCheckService.Verify(x => x.CheckHealthAsync(), Times.Once);
    }
} 