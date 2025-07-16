using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using System.Security.Claims;
using Xunit;
using Normaize.Core.Interfaces;
using Normaize.Data.Services;

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
        _mockHttpContext.Setup(x => x.Items).Returns(new Dictionary<object, object?>());
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
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

        // Act
        _loggingService.LogUserAction(action, data);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("User Action: Test Action")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Action Data")),
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

        // Act
        _loggingService.LogUserAction(action);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("User Action: Test Action")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogUserAction_WithCustomLogLevel_ShouldUseSpecifiedLevel()
    {
        // Arrange
        var action = "Test Action";
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

        // Act
        _loggingService.LogUserAction(action, null, level: LogLevel.Warning);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("User Action: Test Action")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogUserAction_WithNullAction_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogUserAction(null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void LogUserAction_WithEmptyAction_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogUserAction("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
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

        // Act
        _loggingService.LogException(exception, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Exception in Test Context")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogException_WithNullException_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogException(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public void LogException_WithCustomLogLevel_ShouldUseSpecifiedLevel()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = "Test Context";
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

        // Act
        _loggingService.LogException(exception, context, LogLevel.Critical);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Exception in Test Context")),
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

        // Act
        _loggingService.LogRequestStart(method, path, userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Request Started: GET /api/test by User: user123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogRequestStart_WithNullMethod_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogRequestStart(null!, "/api/test");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void LogRequestStart_WithNullPath_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogRequestStart("GET", null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
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

        // Act
        _loggingService.LogRequestEnd(method, path, statusCode, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Request Completed: POST /api/test - Status: 200 - Duration: 150ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogRequestEnd_WithErrorStatusCode_ShouldLogWarning()
    {
        // Arrange
        var method = "POST";
        var path = "/api/test";
        var statusCode = 404;
        var durationMs = 150L;
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

        // Act
        _loggingService.LogRequestEnd(method, path, statusCode, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Request Completed: POST /api/test - Status: 404")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogRequestEnd_WithInvalidStatusCode_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogRequestEnd("GET", "/api/test", 999, 100);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Status code must be between 100 and 599*");
    }

    [Fact]
    public void LogRequestEnd_WithNegativeDuration_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogRequestEnd("GET", "/api/test", 200, -100);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Duration cannot be negative*");
    }

    [Fact]
    public void LogPerformance_WithValidParameters_ShouldLogInformation()
    {
        // Arrange
        var operation = "Database Query";
        var durationMs = 150L;
        var metadata = new { TableName = "Users", QueryType = "SELECT" };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

        // Act
        _loggingService.LogPerformance(operation, durationMs, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Performance: Database Query completed in 150ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Performance Metadata")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPerformance_WithNullMetadata_ShouldLogOnlyPerformance()
    {
        // Arrange
        var operation = "Database Query";
        var durationMs = 150L;
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockHttpContext.Setup(x => x.User).Returns(principal);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

        // Act
        _loggingService.LogPerformance(operation, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Performance: Database Query completed in 150ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPerformance_WithNullOperation_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogPerformance(null!, 100);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void LogPerformance_WithNegativeDuration_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _loggingService.LogPerformance("Test", -100);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Duration cannot be negative*");
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