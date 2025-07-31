using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Normaize.API.Middleware;
using Normaize.Core.Interfaces;
using Normaize.Core.Configuration;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Xunit;

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

        // Setup service provider to return the logging service
        _mockServiceProvider.Setup(x => x.GetService(typeof(IStructuredLoggingService)))
            .Returns(_mockLoggingService.Object);
        _context.RequestServices = _mockServiceProvider.Object;

        // Set up a proper response body stream
        _context.Response.Body = new MemoryStream();

        // Set trace identifier for correlation ID
        _context.TraceIdentifier = "test-trace-id";
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNextMiddleware()
    {
        // Arrange
        var wasNextCalled = false;
        RequestDelegate next = async (ctx) => { await Task.Yield(); wasNextCalled = true; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        wasNextCalled.Should().BeTrue();
        _context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        _context.Response.ContentType.Should().Be("application/json");

        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("Invalid request parameters provided");
        responseBody.Should().Contain("BAD_REQUEST");
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
        responseBody.Should().Contain("UNAUTHORIZED");
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
        responseBody.Should().Contain("INVALID_OPERATION");
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
        responseBody.Should().Contain("NOT_FOUND");
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
        responseBody.Should().Contain("NOT_SUPPORTED");
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
        responseBody.Should().Contain("TIMEOUT");
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
        responseBody.Should().Contain("INTERNAL_SERVER_ERROR");
    }

    [Fact]
    public async Task InvokeAsync_WhenNoTraceIdentifier_ShouldGenerateCorrelationId()
    {
        // Arrange
        _context.TraceIdentifier = string.Empty;
        var exception = new Exception("Test exception");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("correlationId");
        responseBody.Should().NotContain("test-trace-id");
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeRequestContextInResponse()
    {
        // Arrange
        _context.Request.Path = "/api/datasets/123";
        _context.Request.Method = "GET";
        var exception = new ArgumentException("Invalid request");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        var responseBody = await GetResponseBody();
        responseBody.Should().Contain("test-trace-id");
        responseBody.Should().Contain("Invalid request parameters provided");
        responseBody.Should().Contain("BAD_REQUEST");
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogExceptionWithContext()
    {
        // Arrange
        _context.Request.Path = "/api/test";
        _context.Request.Method = "POST";
        var exception = new Exception("Test exception");
        RequestDelegate next = async (ctx) => { await Task.Yield(); throw exception; };

        var middleware = new ExceptionHandlingMiddleware(next);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _mockLoggingService.Verify(
            x => x.LogException(
                exception,
                It.Is<string>(s => s.Contains("Global exception handler") && s.Contains("POST") && s.Contains("/api/test"))
            ),
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
        _context.Response.ContentType.Should().Be("application/json");

        var responseBody = await GetResponseBody();
        responseBody.Should().NotBeNullOrEmpty();

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(responseBody);
        jsonDoc.RootElement.TryGetProperty("success", out var successElement).Should().BeTrue();
        jsonDoc.RootElement.TryGetProperty("message", out var messageElement).Should().BeTrue();
        jsonDoc.RootElement.TryGetProperty("errorCode", out var errorCodeElement).Should().BeTrue();
        jsonDoc.RootElement.TryGetProperty("metadata", out var metadataElement).Should().BeTrue();

        successElement.GetBoolean().Should().BeFalse();
        messageElement.GetString().Should().Contain("Invalid request parameters provided");
        errorCodeElement.GetString().Should().Be("BAD_REQUEST");
    }

    private async Task<string> GetResponseBody()
    {
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}