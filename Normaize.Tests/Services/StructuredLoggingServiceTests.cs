using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.API.Services;
using FluentAssertions;
using System.Security.Claims;
using Xunit;

namespace Normaize.Tests.Services;

public class StructuredLoggingServiceTests
{
    private readonly Mock<ILogger<StructuredLoggingService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockRequest;
    private readonly StructuredLoggingService _loggingService;

    public StructuredLoggingServiceTests()
    {
        _mockLogger = new Mock<ILogger<StructuredLoggingService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        
        // Set up default HttpContext with anonymous user and request
        var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // Empty claims identity
        _mockHttpContext.Setup(x => x.User).Returns(anonymousPrincipal);
        _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
        _mockHttpContext.Setup(x => x.Items).Returns(new Dictionary<object, object>()); // <-- Add this line
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        
        _loggingService = new StructuredLoggingService(_mockLogger.Object, _mockHttpContextAccessor.Object);
    }

    // Patch each test that sets up a different user to re-setup the mock
    [Fact]
    public void LogUserAction_WithValidAction_ShouldLogInformation()
    {
        // Arrange
        var action = "Test Action";
        var data = new { testProperty = "testValue" };
        // Always use ClaimsPrincipal with ClaimsIdentity
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        _loggingService.LogUserAction(action, data);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v!.ToString().Contains("User Action: Test Action")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v!.ToString().Contains("Action Data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogUserAction_WithNullData_ShouldLogOnlyAction()
    {
        // Arrange
        var action = "Test Action";
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        _loggingService.LogUserAction(action);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v!.ToString().Contains("User Action: Test Action")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogException_WithValidException_ShouldLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = "Test Context";
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        _loggingService.LogException(exception, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v!.ToString().Contains("Exception in Test Context")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogRequestStart_WithValidParameters_ShouldLogInformation()
    {
        // Arrange
        var method = "GET";
        var path = "/api/test";
        var userId = "user123";
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        _loggingService.LogRequestStart(method, path, userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v!.ToString().Contains("Request Started: GET /api/test by User: user123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogRequestEnd_WithValidParameters_ShouldLogInformation()
    {
        // Arrange
        var method = "POST";
        var path = "/api/test";
        var statusCode = 200;
        var durationMs = 150L;
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        _loggingService.LogRequestEnd(method, path, statusCode, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v!.ToString().Contains("Request Completed: POST /api/test - Status: 200 - Duration: 150ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateUserScope_WithValidUser_ShouldReturnDisposable()
    {
        // Arrange
        var userId = "user123";
        var userEmail = "user@example.com";
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        using var scope = _loggingService.CreateUserScope(userId, userEmail);

        // Assert
        scope.Should().NotBeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithAuthenticatedUser_ShouldReturnUserId()
    {
        // Arrange
        var expectedUserId = "user123";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId),
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        var method = _loggingService.GetType()
            .GetMethod("GetCurrentUserId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();
        var actualUserId = method!.Invoke(_loggingService, null) as string;

        // Assert
        actualUserId.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetCurrentUserId_WithNoUser_ShouldReturnNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        var method = _loggingService.GetType()
            .GetMethod("GetCurrentUserId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();
        var actualUserId = method!.Invoke(_loggingService, null) as string;

        // Assert
        actualUserId.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserEmail_WithAuthenticatedUser_ShouldReturnEmail()
    {
        // Arrange
        var expectedEmail = "user@example.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Email, expectedEmail)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContext.Object.User: {_mockHttpContext.Object.User}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] _mockHttpContextAccessor.Object.HttpContext?.User: {_mockHttpContextAccessor.Object.HttpContext?.User}");

        // Act
        var method = _loggingService.GetType()
            .GetMethod("GetCurrentUserEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();
        var actualEmail = method!.Invoke(_loggingService, null) as string;

        // Assert
        actualEmail.Should().Be(expectedEmail);
    }
} 