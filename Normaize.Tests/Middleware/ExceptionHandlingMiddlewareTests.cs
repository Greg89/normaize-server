using Microsoft.AspNetCore.Http;
using Moq;
using Normaize.API.Middleware;
using System.Net;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Normaize.Core.Interfaces;

namespace Normaize.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly DefaultHttpContext _context;

    public ExceptionHandlingMiddlewareTests()
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
    public async Task InvokeAsync_WhenNoException_ShouldCallNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = async (ctx) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        nextCalled.Should().BeTrue();
        _context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var exception = new ArgumentException("Invalid parameter");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        _context.Response.ContentType.Should().Be("application/json");

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("Invalid request parameters provided");
        responseBody.Should().Contain("ArgumentException");
        responseBody.Should().Contain("test-trace-id");

        _mockLoggingService.Verify(
            x => x.LogException(exception, It.Is<string>(s => s.Contains("Global exception handler"))),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_ShouldReturnUnauthorized()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Not authorized");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("You are not authorized to perform this action");
        responseBody.Should().Contain("UnauthorizedAccessException");
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("The requested operation cannot be completed");
        responseBody.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var exception = new KeyNotFoundException("Key not found");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("The requested resource was not found");
        responseBody.Should().Contain("KeyNotFoundException");
    }

    [Fact]
    public async Task InvokeAsync_WhenNotSupportedException_ShouldReturnMethodNotAllowed()
    {
        // Arrange
        var exception = new NotSupportedException("Not supported");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.MethodNotAllowed);

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("This operation is not supported");
        responseBody.Should().Contain("NotSupportedException");
    }

    [Fact]
    public async Task InvokeAsync_WhenTimeoutException_ShouldReturnRequestTimeout()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.RequestTimeout);

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("The operation timed out. Please try again");
        responseBody.Should().Contain("TimeoutException");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnknownException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new DivideByZeroException("Division by zero");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("An unexpected error occurred while processing your request");
        responseBody.Should().Contain("DivideByZeroException");
    }

    [Fact]
    public async Task InvokeAsync_WhenNoTraceIdentifier_ShouldGenerateCorrelationId()
    {
        // Arrange
        _context.TraceIdentifier = null!;
        var exception = new ArgumentException("Test exception");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("correlationId");

        // Should contain either a trace identifier format or a GUID format correlation ID
        responseBody.Should().MatchRegex(@"""correlationId"":""[0-9A-Z]{13}|[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}""");
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeRequestContextInResponse()
    {
        // Arrange
        _context.Request.Path = "/api/datasets/123";
        _context.Request.Method = "POST";
        var exception = new ArgumentException("Test exception");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        var responseBody = await GetResponseBody();
        responseBody.Should().Contain(@"""requestPath"":""/api/datasets/123""");
        responseBody.Should().Contain(@"""requestMethod"":""POST""");
        responseBody.Should().Contain("timestamp");
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogExceptionWithContext()
    {
        // Arrange
        _context.Request.Path = "/api/test";
        _context.Request.Method = "PUT";
        var exception = new ArgumentException("Test exception");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogException(
                exception,
                It.Is<string>(s =>
                    s.Contains("Global exception handler") &&
                    s.Contains("PUT") &&
                    s.Contains("/api/test") &&
                    s.Contains("test-trace-id"))),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnValidJsonResponse()
    {
        // Arrange
        var exception = new ArgumentException("Test exception");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        var responseBody = await GetResponseBody();

        // Should be valid JSON
        Action parseJson = () => JsonDocument.Parse(responseBody);
        parseJson.Should().NotThrow();

        // Should have expected structure
        var jsonDoc = JsonDocument.Parse(responseBody);
        jsonDoc.RootElement.TryGetProperty("error", out var errorElement).Should().BeTrue();
        errorElement.TryGetProperty("message", out _).Should().BeTrue();
        errorElement.TryGetProperty("type", out _).Should().BeTrue();
        errorElement.TryGetProperty("details", out _).Should().BeTrue();
        errorElement.TryGetProperty("correlationId", out _).Should().BeTrue();
        errorElement.TryGetProperty("requestPath", out _).Should().BeTrue();
        errorElement.TryGetProperty("requestMethod", out _).Should().BeTrue();
        errorElement.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    private async Task<string> GetResponseBody()
    {
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}