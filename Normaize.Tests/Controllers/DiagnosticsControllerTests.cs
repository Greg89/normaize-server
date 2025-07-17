using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Xunit;
using FluentAssertions;
using Normaize.API.Controllers;

namespace Normaize.Tests.Controllers;

public class DiagnosticsControllerTests
{
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly Mock<IAppConfigurationService> _mockConfigService;
    private readonly DiagnosticsController _controller;

    public DiagnosticsControllerTests()
    {
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _mockConfigService = new Mock<IAppConfigurationService>();
        _controller = new DiagnosticsController(_mockLoggingService.Object, _mockConfigService.Object);
    }

    [Fact]
    public void GetStorageDiagnostics_ReturnsExpectedConfigStatus()
    {
        // Arrange
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", "S3");
        Environment.SetEnvironmentVariable("AWS_S3_BUCKET", "bucket");
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "key");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "secret");
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", "url");
        _mockConfigService.Setup(x => x.GetEnvironment()).Returns("Production");

        // Act
        var result = _controller.GetStorageDiagnostics();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var diagnostics = ok!.Value as StorageDiagnosticsDto;
        diagnostics.Should().NotBeNull();
        diagnostics!.StorageProvider.Should().Be(StorageProvider.S3);
        diagnostics.S3Configured.Should().BeTrue();
        diagnostics.S3Bucket.Should().Be("SET");
        diagnostics.S3AccessKey.Should().Be("SET");
        diagnostics.S3SecretKey.Should().Be("SET");
        diagnostics.S3ServiceUrl.Should().Be("SET");
        diagnostics.Environment.Should().Be("Production");
    }

    [Fact]
    public void GetStorageDiagnostics_WhenException_LogsAndReturns500()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetEnvironment()).Throws(new Exception("fail"));

        // Act
        var result = _controller.GetStorageDiagnostics();

        // Assert
        var obj = result.Result as ObjectResult;
        obj.Should().NotBeNull();
        obj!.StatusCode.Should().Be(500);
        _mockLoggingService.Verify(x => x.LogException(It.IsAny<Exception>(), "GetStorageDiagnostics"), Times.Once);
    }

    // TestStorage endpoint is more of an integration test, but we can check error handling
    [Fact]
    public async Task TestStorage_WhenException_LogsAndReturns500()
    {
        // Arrange
        var controller = new DiagnosticsController(_mockLoggingService.Object, _mockConfigService.Object);
        controller.ControllerContext = new ControllerContext();
        
        // Mock the HttpContext.RequestServices to throw an exception when GetRequiredService is called
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
            .Throws(new InvalidOperationException("Service not found"));
        
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = mockServiceProvider.Object
        };

        // Act
        var result = await controller.TestStorage();

        // Assert
        var actionResult = result as ActionResult<StorageTestResultDto>;
        actionResult.Should().NotBeNull();
        var obj = actionResult!.Result as ObjectResult;
        obj.Should().NotBeNull();
        obj!.StatusCode.Should().Be(500);
        _mockLoggingService.Verify(x => x.LogException(It.IsAny<Exception>(), "TestStorage"), Times.Once);
    }

    [Fact]
    public void GetStorageDiagnostics_WithMissingConfig_ReturnsNotSetStatus()
    {
        // Arrange
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
        Environment.SetEnvironmentVariable("AWS_S3_BUCKET", null);
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", null);
        _mockConfigService.Setup(x => x.GetEnvironment()).Returns(string.Empty);

        // Act
        var result = _controller.GetStorageDiagnostics();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var diagnostics = ok!.Value as StorageDiagnosticsDto;
        diagnostics.Should().NotBeNull();
        diagnostics!.StorageProvider.Should().Be(StorageProvider.Local);
        diagnostics.S3Configured.Should().BeFalse();
        diagnostics.S3Bucket.Should().Be("NOT SET");
        diagnostics.S3AccessKey.Should().Be("NOT SET");
        diagnostics.S3SecretKey.Should().Be("NOT SET");
        diagnostics.S3ServiceUrl.Should().Be("NOT SET");
        diagnostics.Environment.Should().Be("NOT SET");
    }
} 