using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.Data.Services;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using System.Diagnostics;
using System.Reflection;

namespace Normaize.Tests.Services;

public class StructuredLoggingServiceTests
{
    private readonly Mock<ILogger<StructuredLoggingService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockHttpRequest;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly StructuredLoggingService _service;

    public StructuredLoggingServiceTests()
    {
        _mockLogger = new Mock<ILogger<StructuredLoggingService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpRequest = new Mock<HttpRequest>();
        _mockUser = new Mock<ClaimsPrincipal>();

        // Setup HTTP context
        _mockHttpRequest.Setup(r => r.Method).Returns("GET");
        _mockHttpRequest.Setup(r => r.Path).Returns("/api/test");
        _mockHttpContext.Setup(c => c.Request).Returns(_mockHttpRequest.Object);
        _mockHttpContext.Setup(c => c.User).Returns(_mockUser.Object);
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(_mockHttpContext.Object);

        _service = new StructuredLoggingService(_mockLogger.Object, _mockHttpContextAccessor.Object);
    }

    #region Structured Logging Tests

    [Fact]
    public void CreateContext_WithValidParameters_ShouldReturnValidContext()
    {
        // Arrange
        var operationName = "TestOperation";
        var correlationId = "test-correlation-id";
        var userId = "user123";
        var additionalContext = new Dictionary<string, object> { ["Key"] = "Value" };

        // Act
        var context = _service.CreateContext(operationName, correlationId, userId, additionalContext);

        // Assert
        context.Should().NotBeNull();
        context.OperationName.Should().Be(operationName);
        context.CorrelationId.Should().Be(correlationId);
        context.UserId.Should().Be(userId);
        context.Metadata.Should().ContainKey("Key");
        context.Metadata["Key"].Should().Be("Value");
        context.Steps.Should().BeEmpty();
        context.Stopwatch.Should().NotBeNull();
    }

    [Fact]
    public void CreateContext_WithNullUserId_ShouldUseUnknownDefault()
    {
        // Arrange
        var operationName = "TestOperation";
        var correlationId = "test-correlation-id";

        // Act
        var context = _service.CreateContext(operationName, correlationId);

        // Assert
        context.UserId.Should().Be(AppConstants.Messages.UNKNOWN);
    }

    [Fact]
    public void CreateContext_WithNullAdditionalContext_ShouldCreateEmptyMetadata()
    {
        // Arrange
        var operationName = "TestOperation";
        var correlationId = "test-correlation-id";

        // Act
        var context = _service.CreateContext(operationName, correlationId);

        // Assert
        context.Metadata.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "correlation-id")]
    [InlineData("operation", "")]
    public void CreateContext_WithEmptyParameters_ShouldThrowArgumentException(string? operationName, string? correlationId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.CreateContext(operationName!, correlationId!));
    }

    [Theory]
    [InlineData(null, "correlation-id")]
    [InlineData("operation", null)]
    public void CreateContext_WithNullParameters_ShouldThrowArgumentNullException(string? operationName, string? correlationId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _service.CreateContext(operationName!, correlationId!));
    }

    [Fact]
    public void LogStep_ShouldAddStepAndMetadata()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");
        var step = "Test Step";
        var additionalData = new Dictionary<string, object> { ["DataKey"] = "DataValue" };

        // Act
        _service.LogStep(context, step, additionalData);

        // Assert
        context.Steps.Should().ContainSingle();
        context.Steps[0].Should().Be(step);
        context.Metadata.Should().ContainKey("DataKey");
        context.Metadata["DataKey"].Should().Be("DataValue");
    }

    [Fact]
    public void LogStep_WithNullAdditionalData_ShouldOnlyAddStep()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");
        var step = "Test Step";

        // Act
        _service.LogStep(context, step);

        // Assert
        context.Steps.Should().ContainSingle();
        context.Steps[0].Should().Be(step);
        context.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void LogStep_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _service.LogStep(null!, "step"));
        
        exception.ParamName.Should().Be("context");
    }

    [Theory]
    [InlineData("")]
    public void LogStep_WithEmptyStep_ShouldThrowArgumentException(string? step)
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.LogStep(context, step!));
    }

    [Fact]
    public void LogStep_WithNullStep_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _service.LogStep(context, null!));
    }

    [Fact]
    public void LogSummary_WithSuccess_ShouldLogInformation()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");
        _service.LogStep(context, "Step 1");
        _service.LogStep(context, "Step 2", new Dictionary<string, object> { ["Key"] = "Value" });

        // Act
        _service.LogSummary(context, true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSummary_WithFailure_ShouldLogError()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");
        var errorMessage = "Test error message";

        // Act
        _service.LogSummary(context, false, errorMessage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSummary_WithNullErrorMessage_ShouldUseUnknownDefault()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");

        // Act
        _service.LogSummary(context, false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogImmediateStep_ShouldLogImmediately()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");
        var step = "Immediate Step";
        var additionalData = new Dictionary<string, object> { ["ImmediateKey"] = "ImmediateValue" };

        // Act
        _service.LogImmediateStep(context, step, LogLevel.Debug, additionalData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation step: Immediate Step")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogImmediateStep_WithDefaultLevel_ShouldUseInformation()
    {
        // Arrange
        var context = _service.CreateContext("TestOperation", "test-correlation-id");
        var step = "Default Level Step";

        // Act
        _service.LogImmediateStep(context, step);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation step: Default Level Step")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void LogUserAction_WithValidAction_ShouldLogInformation()
    {
        // Arrange
        var action = "TestAction";
        var data = new { TestProperty = "TestValue" };

        // Act
        _service.LogUserAction(action, data);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User Action: TestAction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogUserAction_WithCustomLevel_ShouldLogAtSpecifiedLevel()
    {
        // Arrange
        var action = "TestAction";

        // Act
        _service.LogUserAction(action, null, LogLevel.Warning);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User Action: TestAction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogException_WithValidException_ShouldLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = "TestContext";

        // Act
        _service.LogException(exception, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception in TestContext")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogException_WithCustomLevel_ShouldLogAtSpecifiedLevel()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = "TestContext";

        // Act
        _service.LogException(exception, context, LogLevel.Critical);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception in TestContext")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogException_WithNullException_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            _service.LogException(null!, "context"));
        
        exception.ParamName.Should().Be("exception");
    }

    [Fact]
    public void LogRequestStart_WithValidParameters_ShouldLogInformation()
    {
        // Arrange
        var method = "POST";
        var path = "/api/data";
        var userId = "user123";

        // Act
        _service.LogRequestStart(method, path, userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request Started: POST /api/data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogRequestEnd_WithValidParameters_ShouldLogInformation()
    {
        // Arrange
        var method = "GET";
        var path = "/api/health";
        var statusCode = 200;
        var durationMs = 150L;
        var userId = "user123";

        // Act
        _service.LogRequestEnd(method, path, statusCode, durationMs, userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request Completed: GET /api/health")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogRequestEnd_WithErrorStatusCode_ShouldLogWarning()
    {
        // Arrange
        var method = "POST";
        var path = "/api/data";
        var statusCode = 500;
        var durationMs = 2000L;

        // Act
        _service.LogRequestEnd(method, path, statusCode, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request Completed: POST /api/data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPerformance_WithValidParameters_ShouldLogInformation()
    {
        // Arrange
        var operation = "DatabaseQuery";
        var durationMs = 500L;
        var metadata = new { TableName = "Users", QueryType = "SELECT" };

        // Act
        _service.LogPerformance(operation, durationMs, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performance: DatabaseQuery completed in 500ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateUserScope_ShouldReturnDisposable()
    {
        // Act
        var scope = _service.CreateUserScope("user123", "user@example.com");

        // Assert
        scope.Should().NotBeNull();
        scope.Should().BeAssignableTo<IDisposable>();
        
        // Should not throw when disposed
        var action = () => scope.Dispose();
        action.Should().NotThrow();
    }

    #endregion

    #region User Context Tests

    [Fact]
    public void GetCurrentUserId_WithNameIdentifierClaim_ShouldReturnClaimValue()
    {
        // Arrange
        var expectedUserId = "auth0|123456";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, expectedUserId)
        };
        _mockUser.Setup(u => u.FindFirst(ClaimTypes.NameIdentifier)).Returns(claims[0]);

        // Act
        var service = new StructuredLoggingService(_mockLogger.Object, _mockHttpContextAccessor.Object);
        var result = service.GetType().GetMethod("GetCurrentUserId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, null) as string;

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetCurrentUserId_WithSubClaim_ShouldReturnClaimValue()
    {
        // Arrange
        var expectedUserId = "auth0|123456";
        var claims = new List<Claim>
        {
            new("sub", expectedUserId)
        };
        _mockUser.Setup(u => u.FindFirst("sub")).Returns(claims[0]);

        // Act
        var service = new StructuredLoggingService(_mockLogger.Object, _mockHttpContextAccessor.Object);
        var result = service.GetType().GetMethod("GetCurrentUserId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, null) as string;

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetCurrentUserEmail_WithEmailClaim_ShouldReturnClaimValue()
    {
        // Arrange
        var expectedEmail = "user@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, expectedEmail)
        };
        _mockUser.Setup(u => u.FindFirst(ClaimTypes.Email)).Returns(claims[0]);

        // Act
        var service = new StructuredLoggingService(_mockLogger.Object, _mockHttpContextAccessor.Object);
        var result = service.GetType().GetMethod("GetCurrentUserEmail", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, null) as string;

        // Assert
        result.Should().Be(expectedEmail);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateInput_WithInvalidValue_ShouldThrowArgumentException(string? value)
    {
        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => 
            _service.GetType().GetMethod("ValidateInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { value!, "parameterName" }));
        
        exception.InnerException.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void ValidateStatusCode_WithInvalidStatusCode_ShouldThrowArgumentException(int statusCode)
    {
        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => 
            _service.GetType().GetMethod("ValidateStatusCode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { statusCode }));
        
        exception.InnerException.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public void ValidateDuration_WithNegativeDuration_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => 
            _service.GetType().GetMethod("ValidateDuration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { -1L }));
        
        exception.InnerException.Should().BeOfType<ArgumentException>();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteOperationFlow_ShouldWorkEndToEnd()
    {
        // Arrange
        var context = _service.CreateContext("CompleteTest", "test-correlation-id", "user123");
        
        // Act - Simulate a complete operation
        _service.LogStep(context, "Step 1: Initialization");
        _service.LogStep(context, "Step 2: Processing", new Dictionary<string, object> 
        { 
            ["ProcessedItems"] = 100,
            ["ProcessingTime"] = 1500 
        });
        _service.LogStep(context, "Step 3: Validation");
        _service.LogSummary(context, true);

        // Assert
        context.Steps.Should().HaveCount(3);
        context.Steps.Should().ContainInOrder("Step 1: Initialization", "Step 2: Processing", "Step 3: Validation");
        context.Metadata.Should().ContainKey("ProcessedItems");
        context.Metadata.Should().ContainKey("ProcessingTime");
        context.Metadata["ProcessedItems"].Should().Be(100);
        context.Metadata["ProcessingTime"].Should().Be(1500);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OperationContext_MetadataOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var context = _service.CreateContext("MetadataTest", "test-correlation-id");

        // Act
        context.SetMetadata("Key1", "Value1");
        context.SetMetadata("Key2", 42);
        context.SetMetadata("Key3", true);

        // Assert
        context.GetMetadata<string>("Key1").Should().Be("Value1");
        context.GetMetadata<int>("Key2").Should().Be(42);
        context.GetMetadata<bool>("Key3").Should().Be(true);
        context.GetMetadata<string>("NonExistentKey").Should().BeNull();
        context.GetMetadata<string>("NonExistentKey", "default").Should().Be("default");
    }

    #endregion
} 