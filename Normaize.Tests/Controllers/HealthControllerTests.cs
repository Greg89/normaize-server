using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
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

        // Set up controller context to avoid null reference issues
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void Get_ShouldReturnOkResultWithHealthStatus()
    {
        // Act
        var result = _controller.Get();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<HealthResponseDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<HealthResponseDto>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var response = apiResponse.Data!;
        // Check properties
        response.Status.Should().NotBeNull();
        response.Timestamp.Should().NotBe(default(DateTime));
        response.Service.Should().NotBeNull();
        response.Version.Should().NotBeNull();
        response.Environment.Should().NotBeNull();
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
        result.Should().BeOfType<ActionResult<ApiResponse<HealthResponseDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<HealthResponseDto>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var response = apiResponse.Data!;

        response.Status.Should().Be("healthy");
        response.Timestamp.Should().NotBe(default(DateTime));
        response.Service.Should().Be("Normaize API");
        response.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Get_ShouldIncludeEnvironmentInformation()
    {
        // Act
        var result = _controller.Get();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<HealthResponseDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<HealthResponseDto>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var response = apiResponse.Data!;

        response.Environment.Should().NotBeNull();
        response.Environment.Should().NotBeEmpty();
    }
}