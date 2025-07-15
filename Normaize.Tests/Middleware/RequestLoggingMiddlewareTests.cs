using Microsoft.AspNetCore.Http;
using Moq;
using Normaize.API.Middleware;
using Normaize.API.Services;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Middleware;

public class RequestLoggingMiddlewareTests
{
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly DefaultHttpContext _context;

    public RequestLoggingMiddlewareTests()
    {
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _context = new DefaultHttpContext();

        // Set up a proper response body stream
        _context.Response.Body = new MemoryStream();

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IStructuredLoggingService)))
            .Returns(_mockLoggingService.Object);

        _context.RequestServices = _mockServiceProvider.Object;
        _context.Request.Path = "/api/test";
        _context.Request.Method = "GET";
        _context.TraceIdentifier = "test-trace-id";
    }

    [Fact]
    public async Task InvokeAsync_WhenSuccessfulRequest_ShouldLogStartAndEnd()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = async (ctx) => 
        {
            nextCalled = true;
            await Task.CompletedTask;
        };

        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        nextCalled.Should().BeTrue();
        _context.Response.StatusCode.Should().Be(200);

        _mockLoggingService.Verify(
            x => x.LogRequestStart("GET", "/api/test", null),
            Times.Once);

        _mockLoggingService.Verify(
            x => x.LogRequestEnd("GET", "/api/test", 200, It.IsAny<long>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserFromClaims_ShouldLogWithUserId()
    {
        // Arrange
        var userId = "user-123";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        _context.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

        RequestDelegate next = async (ctx) => await Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogRequestStart("GET", "/api/test", userId),
            Times.Once);

        _mockLoggingService.Verify(
            x => x.LogRequestEnd("GET", "/api/test", 200, It.IsAny<long>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserFromContextItems_ShouldLogWithUserId()
    {
        // Arrange
        var userId = "user-456";
        _context.Items["UserId"] = userId;

        RequestDelegate next = async (ctx) => await Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogRequestStart("GET", "/api/test", userId),
            Times.Once);

        _mockLoggingService.Verify(
            x => x.LogRequestEnd("GET", "/api/test", 200, It.IsAny<long>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserInBothClaimsAndItems_ShouldPreferClaims()
    {
        // Arrange
        var claimsUserId = "user-from-claims";
        var itemsUserId = "user-from-items";
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, claimsUserId)
        };
        _context.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _context.Items["UserId"] = itemsUserId;

        RequestDelegate next = async (ctx) => await Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogRequestStart("GET", "/api/test", claimsUserId),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldLogExceptionAndReThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new RequestLoggingMiddleware(next);

        // Act & Assert
        var action = () => middleware.InvokeAsync(_context);
        await action.Should().ThrowAsync<InvalidOperationException>();

        _mockLoggingService.Verify(
            x => x.LogRequestStart("GET", "/api/test", null),
            Times.Once);

        _mockLoggingService.Verify(
            x => x.LogException(exception, "Request processing failed: GET /api/test"),
            Times.Once);

        // Should not log request end when exception occurs
        _mockLoggingService.Verify(
            x => x.LogRequestEnd(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenNullUser_ShouldHandleGracefully()
    {
        // Arrange
        _context.User = null!;

        RequestDelegate next = async (ctx) => await Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogRequestStart("GET", "/api/test", null),
            Times.Once);

        _mockLoggingService.Verify(
            x => x.LogRequestEnd("GET", "/api/test", 200, It.IsAny<long>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenDifferentHttpMethods_ShouldLogCorrectly()
    {
        // Arrange
        _context.Request.Method = "POST";
        _context.Request.Path = "/api/datasets";

        RequestDelegate next = async (ctx) => await Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogRequestStart("POST", "/api/datasets", null),
            Times.Once);

        _mockLoggingService.Verify(
            x => x.LogRequestEnd("POST", "/api/datasets", 200, It.IsAny<long>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenNonSuccessStatusCode_ShouldLogCorrectly()
    {
        // Arrange
        RequestDelegate next = async (ctx) => 
        {
            ctx.Response.StatusCode = 404;
            await Task.CompletedTask;
        };

        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogRequestStart("GET", "/api/test", null),
            Times.Once);

        _mockLoggingService.Verify(
            x => x.LogRequestEnd("GET", "/api/test", 404, It.IsAny<long>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldMeasureRequestDuration()
    {
        // Arrange
        RequestDelegate next = async (ctx) => 
        {
            await Task.Delay(50); // Simulate some processing time
        };

        var middleware = new RequestLoggingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogRequestEnd("GET", "/api/test", 200, It.Is<long>(duration => duration >= 50)),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldStillMeasureDuration()
    {
        // Arrange
        var exception = new Exception("Test exception");
        RequestDelegate next = async (ctx) => 
        {
            await Task.Delay(25);
            throw exception;
        };

        var middleware = new RequestLoggingMiddleware(next);

        // Act & Assert
        var action = () => middleware.InvokeAsync(_context);
        await action.Should().ThrowAsync<Exception>();

        _mockLoggingService.Verify(
            x => x.LogException(exception, It.Is<string>(s => s.Contains("Request processing failed"))),
            Times.Once);
    }
} 