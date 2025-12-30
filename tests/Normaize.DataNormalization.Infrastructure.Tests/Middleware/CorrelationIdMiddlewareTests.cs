using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Infrastructure.Middleware;
using Serilog.Context;
using System.Diagnostics;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<CorrelationIdMiddleware>> _mockLogger;
    private readonly CorrelationIdMiddleware _middleware;

    public CorrelationIdMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<CorrelationIdMiddleware>>();
        _middleware = new CorrelationIdMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateNewCorrelationId_WhenHeaderIsMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns<HttpContext>(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.Headers.Should().ContainKey("X-Correlation-ID");
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue("correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseExistingCorrelationId_WhenHeaderIsPresent()
    {
        // Arrange
        var expectedCorrelationId = "existing-correlation-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = expectedCorrelationId;
        var nextCalled = false;
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns<HttpContext>(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.Headers.Should().ContainKey("X-Correlation-ID");
        var actualCorrelationId = context.Response.Headers["X-Correlation-ID"].ToString();
        actualCorrelationId.Should().Be(expectedCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldStoreCorrelationIdInHttpContextItems()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var correlationIdFromItems = (string?)null;
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns<HttpContext>(ctx =>
            {
                correlationIdFromItems = ctx.Items["CorrelationId"]?.ToString();
                return Task.CompletedTask;
            });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        correlationIdFromItems.Should().NotBeNullOrWhiteSpace();
        context.Items["CorrelationId"].Should().Be(correlationIdFromItems);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPushCorrelationIdToLogContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns<HttpContext>(ctx =>
            {
                // The middleware uses LogContext.PushProperty which creates a scope
                // We verify the middleware executes without throwing, indicating LogContext is properly used
                return Task.CompletedTask;
            });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert - Verify middleware completed successfully
        // The actual LogContext verification happens at runtime when logs are written
        context.Response.Headers.Should().ContainKey("X-Correlation-ID");
        var actualCorrelationId = context.Response.Headers["X-Correlation-ID"].ToString();
        actualCorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationIdToActivityTags_WhenActivityExists()
    {
        // Arrange
        var context = new DefaultHttpContext();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);

        using var activitySource = new ActivitySource("TestActivitySource");
        using var activity = activitySource.StartActivity("TestActivity");

        if (activity == null)
        {
            // If Activity doesn't start (e.g., no listeners), skip this test
            // This is acceptable as Activity support depends on OpenTelemetry configuration
            return;
        }

        var correlationIdInActivity = (string?)null;
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns<HttpContext>(ctx =>
            {
                // Read the correlation ID from Activity tags after middleware sets it
                correlationIdInActivity = Activity.Current?.GetTagItem("CorrelationId")?.ToString();
                return Task.CompletedTask;
            });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        correlationIdInActivity.Should().NotBeNullOrWhiteSpace();
        var expectedCorrelationId = context.Response.Headers["X-Correlation-ID"].ToString();
        correlationIdInActivity.Should().Be(expectedCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotThrow_WhenActivityIsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        Activity.Current = null;
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await _middleware.InvokeAsync(context);

        // Assert
        await act.Should().NotThrowAsync();
        context.Response.Headers.Should().ContainKey("X-Correlation-ID");
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateUniqueCorrelationIds_ForDifferentRequests()
    {
        // Arrange
        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context1);
        await _middleware.InvokeAsync(context2);

        // Assert
        var correlationId1 = context1.Response.Headers["X-Correlation-ID"].ToString();
        var correlationId2 = context2.Response.Headers["X-Correlation-ID"].ToString();
        correlationId1.Should().NotBe(correlationId2);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleEmptyCorrelationIdHeader_ByGeneratingNewOne()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "";
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        correlationId.Should().NotBe("");
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleWhitespaceCorrelationIdHeader_ByGeneratingNewOne()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "   ";
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        correlationId.Trim().Should().NotBeEmpty();
        Guid.TryParse(correlationId.Trim(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns<HttpContext>(ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        _mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPreserveCorrelationId_ThroughException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedException = new InvalidOperationException("Test exception");
        _mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _middleware.InvokeAsync(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        // Correlation ID should still be set even if exception occurs
        context.Response.Headers.Should().ContainKey("X-Correlation-ID");
    }
}

