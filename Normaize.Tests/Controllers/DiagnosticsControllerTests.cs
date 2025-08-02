using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly Mock<IStorageConfigurationService> _mockStorageConfigService;
    private readonly DiagnosticsController _controller;

    public DiagnosticsControllerTests()
    {
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _mockStorageConfigService = new Mock<IStorageConfigurationService>();
        _controller = new DiagnosticsController(_mockLoggingService.Object, _mockStorageConfigService.Object);

        // Set up controller context to avoid null reference issues
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetStorageDiagnostics_ReturnsExpectedConfigStatus()
    {
        // Arrange
        var expectedDiagnostics = new StorageDiagnosticsDto
        {
            StorageProvider = StorageProvider.S3,
            S3Configured = true,
            S3Bucket = "SET",
            S3AccessKey = "SET",
            S3SecretKey = "SET",
            S3ServiceUrl = "SET",
            Environment = "Production"
        };

        _mockStorageConfigService.Setup(x => x.GetDiagnostics()).Returns(expectedDiagnostics);

        // Act
        var result = await _controller.GetStorageDiagnostics();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<StorageDiagnosticsDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<StorageDiagnosticsDto>>().Subject!;
        var diagnostics = apiResponse.Data;
        diagnostics.Should().NotBeNull();
        diagnostics!.StorageProvider.Should().Be(StorageProvider.S3);
        diagnostics.S3Configured.Should().BeTrue();
        diagnostics.S3Bucket.Should().Be("SET");
        diagnostics.S3AccessKey.Should().Be("SET");
        diagnostics.S3SecretKey.Should().Be("SET");
        diagnostics.S3ServiceUrl.Should().Be("SET");
        diagnostics.Environment.Should().Be("Production");

        // Verify logging was called
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics requested", It.IsAny<object>()), Times.Once);
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics retrieved successfully", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetStorageDiagnostics_WhenException_LogsAndReturns500()
    {
        // Arrange
        _mockStorageConfigService.Setup(x => x.GetDiagnostics()).Throws(new Exception("fail"));

        // Act
        var result = await _controller.GetStorageDiagnostics();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<StorageDiagnosticsDto>>>();
        var obj = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        obj.StatusCode.Should().Be(500);
        _mockLoggingService.Verify(x => x.LogException(It.IsAny<Exception>(), "GetStorageDiagnostics"), Times.Once);
    }

    // TestStorage endpoint is more of an integration test, but we can check error handling
    [Fact]
    public async Task TestStorage_WhenException_LogsAndReturns500()
    {
        // Arrange
        var controller = new DiagnosticsController(_mockLoggingService.Object, _mockStorageConfigService.Object);
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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<StorageTestResultDto>>>().Subject;
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(400);

        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<StorageTestResultDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("Service not found");

        _mockLoggingService.Verify(x => x.LogException(It.IsAny<Exception>(), "TestStorage"), Times.Once);
    }

    [Fact]
    public async Task GetStorageDiagnostics_WithMissingConfig_ReturnsNotSetStatus()
    {
        // Arrange
        var expectedDiagnostics = new StorageDiagnosticsDto
        {
            StorageProvider = StorageProvider.Local,
            S3Configured = false,
            S3Bucket = "NOT SET",
            S3AccessKey = "NOT SET",
            S3SecretKey = "NOT SET",
            S3ServiceUrl = "NOT SET",
            Environment = "NOT SET"
        };

        _mockStorageConfigService.Setup(x => x.GetDiagnostics()).Returns(expectedDiagnostics);

        // Act
        var result = await _controller.GetStorageDiagnostics();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<StorageDiagnosticsDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<StorageDiagnosticsDto>>().Subject!;
        var diagnostics = apiResponse.Data;
        diagnostics.Should().NotBeNull();
        diagnostics!.StorageProvider.Should().Be(StorageProvider.Local);
        diagnostics.S3Configured.Should().BeFalse();
        diagnostics.S3Bucket.Should().Be("NOT SET");
        diagnostics.S3AccessKey.Should().Be("NOT SET");
        diagnostics.S3SecretKey.Should().Be("NOT SET");
        diagnostics.S3ServiceUrl.Should().Be("NOT SET");
        diagnostics.Environment.Should().Be("NOT SET");

        // Verify logging was called
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics requested", It.IsAny<object>()), Times.Once);
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics retrieved successfully", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData(StorageProvider.S3)]
    [InlineData(StorageProvider.Azure)]
    [InlineData(StorageProvider.Memory)]
    [InlineData(StorageProvider.Local)]
    public async Task GetStorageDiagnostics_WithDifferentProviders_ReturnsCorrectProvider(StorageProvider provider)
    {
        // Arrange
        var expectedDiagnostics = new StorageDiagnosticsDto
        {
            StorageProvider = provider,
            S3Configured = provider == StorageProvider.S3,
            S3Bucket = provider == StorageProvider.S3 ? "SET" : "NOT SET",
            S3AccessKey = provider == StorageProvider.S3 ? "SET" : "NOT SET",
            S3SecretKey = provider == StorageProvider.S3 ? "SET" : "NOT SET",
            S3ServiceUrl = provider == StorageProvider.S3 ? "SET" : "NOT SET",
            Environment = "Test"
        };

        _mockStorageConfigService.Setup(x => x.GetDiagnostics()).Returns(expectedDiagnostics);

        // Act
        var result = await _controller.GetStorageDiagnostics();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<StorageDiagnosticsDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<StorageDiagnosticsDto>>().Subject!;
        var diagnostics = apiResponse.Data;
        diagnostics.Should().NotBeNull();
        diagnostics!.StorageProvider.Should().Be(provider);

        // Verify logging was called
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics requested", It.IsAny<object>()), Times.Once);
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics retrieved successfully", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetStorageDiagnostics_WithCancellationToken_HandlesCancellation()
    {
        // Arrange
        var expectedDiagnostics = new StorageDiagnosticsDto
        {
            StorageProvider = StorageProvider.Local,
            S3Configured = false,
            S3Bucket = "NOT SET",
            S3AccessKey = "NOT SET",
            S3SecretKey = "NOT SET",
            S3ServiceUrl = "NOT SET",
            Environment = "Test"
        };

        _mockStorageConfigService.Setup(x => x.GetDiagnostics()).Returns(expectedDiagnostics);
        var cancellationToken = new CancellationToken();

        // Act
        var result = await _controller.GetStorageDiagnostics(cancellationToken);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<StorageDiagnosticsDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<StorageDiagnosticsDto>>().Subject!;
        var diagnostics = apiResponse.Data;
        diagnostics.Should().NotBeNull();

        // Verify logging was called
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics requested", It.IsAny<object>()), Times.Once);
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics retrieved successfully", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task TestStorage_WithCancellationToken_HandlesCancellation()
    {
        // Arrange
        var mockStorageService = new Mock<IStorageService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(IStorageService))).Returns(mockStorageService.Object);

        var controller = new DiagnosticsController(_mockLoggingService.Object, _mockStorageConfigService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = mockServiceProvider.Object
            }
        };

        var cancellationToken = new CancellationToken();

        // Act
        var result = await controller.TestStorage(cancellationToken);

        // Assert
        result.Should().NotBeNull();
        _mockLoggingService.Verify(x => x.LogUserAction("Storage test requested", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetStorageDiagnostics_WithNullUser_HandlesGracefully()
    {
        // Arrange
        var expectedDiagnostics = new StorageDiagnosticsDto
        {
            StorageProvider = StorageProvider.Local,
            S3Configured = false,
            S3Bucket = "NOT SET",
            S3AccessKey = "NOT SET",
            S3SecretKey = "NOT SET",
            S3ServiceUrl = "NOT SET",
            Environment = "Test"
        };

        _mockStorageConfigService.Setup(x => x.GetDiagnostics()).Returns(expectedDiagnostics);

        // Set up controller with null user identity
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetStorageDiagnostics();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<StorageDiagnosticsDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<StorageDiagnosticsDto>>().Subject!;
        var diagnostics = apiResponse.Data;
        diagnostics.Should().NotBeNull();

        // Verify logging was called with null user
        _mockLoggingService.Verify(x => x.LogUserAction("Storage diagnostics requested", It.Is<object>(o => o != null && o.ToString() != null && o.ToString()!.Contains("UserId"))), Times.Once);
    }

    [Fact]
    public async Task TestStorage_WithNullUser_HandlesGracefully()
    {
        // Arrange
        var mockStorageService = new Mock<IStorageService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(IStorageService))).Returns(mockStorageService.Object);

        var controller = new DiagnosticsController(_mockLoggingService.Object, _mockStorageConfigService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = mockServiceProvider.Object
            }
        };

        // Act
        var result = await controller.TestStorage();

        // Assert
        result.Should().NotBeNull();
        _mockLoggingService.Verify(x => x.LogUserAction("Storage test requested", It.Is<object>(o => o != null && o.ToString() != null && o.ToString()!.Contains("UserId"))), Times.Once);
    }
}