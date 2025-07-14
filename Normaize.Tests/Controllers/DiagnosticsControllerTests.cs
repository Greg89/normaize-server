using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System;
using Normaize.API.Services;

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
        _mockConfigService.Setup(x => x.Get("STORAGE_PROVIDER")).Returns("S3");
        _mockConfigService.Setup(x => x.Get("AWS_S3_BUCKET")).Returns("bucket");
        _mockConfigService.Setup(x => x.Get("AWS_ACCESS_KEY_ID")).Returns("key");
        _mockConfigService.Setup(x => x.Get("AWS_SECRET_ACCESS_KEY")).Returns("secret");
        _mockConfigService.Setup(x => x.Get("AWS_SERVICE_URL")).Returns("url");
        _mockConfigService.Setup(x => x.Get("ASPNETCORE_ENVIRONMENT")).Returns("Production");

        // Act
        var result = _controller.GetStorageDiagnostics();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var diagnostics = ok!.Value as StorageDiagnosticsDto;
        diagnostics.Should().NotBeNull();
        diagnostics!.StorageProvider.Should().Be("S3");
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
        var ex = new Exception("fail");
        _mockConfigService.Setup(x => x.Get(It.IsAny<string>())).Throws(ex);

        // Act
        var result = _controller.GetStorageDiagnostics();

        // Assert
        var obj = result.Result as ObjectResult;
        obj.Should().NotBeNull();
        obj!.StatusCode.Should().Be(500);
        _mockLoggingService.Verify(x => x.LogException(ex, "GetStorageDiagnostics"), Times.Once);
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
    public void GetStorageDiagnostics_WhenConfigServiceThrowsException_LogsAndReturns500()
    {
        // Arrange
        var ex = new Exception("Configuration service failed");
        _mockConfigService.Setup(x => x.Get(It.IsAny<string>())).Throws(ex);

        // Act
        var result = _controller.GetStorageDiagnostics();

        // Assert
        var obj = result.Result as ObjectResult;
        obj.Should().NotBeNull();
        obj!.StatusCode.Should().Be(500);
        _mockLoggingService.Verify(x => x.LogException(ex, "GetStorageDiagnostics"), Times.Once);
    }

    [Fact]
    public void GetStorageDiagnostics_WithMissingConfig_ReturnsNotSetStatus()
    {
        // Arrange
        _mockConfigService.Setup(x => x.Get("STORAGE_PROVIDER")).Returns((string?)null);
        _mockConfigService.Setup(x => x.Get("AWS_S3_BUCKET")).Returns((string?)null);
        _mockConfigService.Setup(x => x.Get("AWS_ACCESS_KEY_ID")).Returns((string?)null);
        _mockConfigService.Setup(x => x.Get("AWS_SECRET_ACCESS_KEY")).Returns((string?)null);
        _mockConfigService.Setup(x => x.Get("AWS_SERVICE_URL")).Returns((string?)null);
        _mockConfigService.Setup(x => x.Get("ASPNETCORE_ENVIRONMENT")).Returns((string?)null);

        // Act
        var result = _controller.GetStorageDiagnostics();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var diagnostics = ok!.Value as StorageDiagnosticsDto;
        diagnostics.Should().NotBeNull();
        diagnostics!.StorageProvider.Should().Be("default");
        diagnostics.S3Configured.Should().BeFalse();
        diagnostics.S3Bucket.Should().Be("NOT SET");
        diagnostics.S3AccessKey.Should().Be("NOT SET");
        diagnostics.S3SecretKey.Should().Be("NOT SET");
        diagnostics.S3ServiceUrl.Should().Be("NOT SET");
        diagnostics.Environment.Should().Be("NOT SET");
    }
} 