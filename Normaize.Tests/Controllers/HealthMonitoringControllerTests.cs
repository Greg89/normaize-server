using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Controllers;

public class HealthMonitoringControllerTests
{
    private readonly Mock<IHealthCheckService> _mockHealthCheckService;
    private readonly HealthMonitoringController _controller;

    public HealthMonitoringControllerTests()
    {
        _mockHealthCheckService = new Mock<IHealthCheckService>();
        _controller = new HealthMonitoringController(_mockHealthCheckService.Object);

        // Set up controller context to avoid null reference issues
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
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

        _mockHealthCheckService.Setup(x => x.CheckLivenessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthyResult);

        // Act
        var result = await _controller.GetLiveness(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject!;
        apiResponse.Data.Should().NotBeNull();

        // The data is an anonymous object, so we need to use reflection to check properties
        var response = apiResponse.Data!;
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

        _mockHealthCheckService.Setup(x => x.CheckLivenessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unhealthyResult);

        // Act
        var result = await _controller.GetLiveness(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(503);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<object>>().Subject!;
        // For error responses, Data is null, so we check the message instead
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNull();
        apiResponse.Message.Should().Contain("Liveness check failed");
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

        _mockHealthCheckService.Setup(x => x.CheckReadinessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthyResult);

        // Act
        var result = await _controller.GetReadiness(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var response = apiResponse.Data!;
        var responseType = response!.GetType();
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
                    Duration = TimeSpan.FromMilliseconds(120.5),
                    ErrorMessage = "Database connection failed"
                }
            },
            Issues = new List<string> { "Database is not ready" }
        };

        _mockHealthCheckService.Setup(x => x.CheckReadinessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unhealthyResult);

        // Act
        var result = await _controller.GetReadiness(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(503);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<object>>().Subject!;
        // For error responses, Data is null, so we check the message instead
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNull();
        apiResponse.Message.Should().Contain("Readiness check failed");
    }

    [Fact]
    public async Task GetHealth_WhenHealthy_ShouldReturnOkResult()
    {
        // Arrange
        var healthyResult = new HealthCheckResult
        {
            IsHealthy = true,
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(250.3),
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
                    Duration = TimeSpan.FromMilliseconds(59.5)
                }
            }
        };

        _mockHealthCheckService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthyResult);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var response = apiResponse.Data!;
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
    public async Task GetHealth_WhenUnhealthy_ShouldReturnServiceUnavailable()
    {
        // Arrange
        var unhealthyResult = new HealthCheckResult
        {
            IsHealthy = false,
            Status = "unhealthy",
            Timestamp = DateTime.UtcNow,
            Duration = TimeSpan.FromMilliseconds(250.3),
            Components = new Dictionary<string, ComponentHealth>
            {
                ["database"] = new ComponentHealth
                {
                    IsHealthy = false,
                    Status = "unhealthy",
                    Duration = TimeSpan.FromMilliseconds(120.5),
                    ErrorMessage = "Database connection failed"
                },
                ["external_services"] = new ComponentHealth
                {
                    IsHealthy = false,
                    Status = "unhealthy",
                    Duration = TimeSpan.FromMilliseconds(59.5),
                    ErrorMessage = "External API timeout"
                }
            },
            Issues = new List<string> { "Database is not responding", "External services are down" }
        };

        _mockHealthCheckService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(unhealthyResult);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(503);

        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<object>>().Subject!;
        // For error responses, Data is null, so we check the message instead
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNull();
        apiResponse.Message.Should().Contain("Health check failed");
    }

    [Fact]
    public async Task GetLiveness_ShouldCallHealthCheckService()
    {
        // Arrange
        var expectedResult = new HealthCheckResult { IsHealthy = true, Status = "alive" };
        _mockHealthCheckService.Setup(x => x.CheckLivenessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetLiveness(CancellationToken.None);

        // Assert
        _mockHealthCheckService.Verify(x => x.CheckLivenessAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReadiness_ShouldCallHealthCheckService()
    {
        // Arrange
        var expectedResult = new HealthCheckResult { IsHealthy = true, Status = "ready" };
        _mockHealthCheckService.Setup(x => x.CheckReadinessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetReadiness(CancellationToken.None);

        // Assert
        _mockHealthCheckService.Verify(x => x.CheckReadinessAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHealth_ShouldCallHealthCheckService()
    {
        // Arrange
        var expectedResult = new HealthCheckResult { IsHealthy = true, Status = "healthy" };
        _mockHealthCheckService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetHealth(CancellationToken.None);

        // Assert
        _mockHealthCheckService.Verify(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}