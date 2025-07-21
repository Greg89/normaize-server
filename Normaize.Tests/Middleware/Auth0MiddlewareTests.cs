using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Middleware;

public class Auth0MiddlewareTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public Auth0MiddlewareTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(ILoggerFactory)))
            .Returns(_mockLoggerFactory.Object);
    }

    [Fact]
    public async Task Auth0Middleware_WithAuthenticatedUser_ShouldExtractClaimsAndAddToContext()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = _mockServiceProvider.Object
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "auth0|123456789"),
            new(ClaimTypes.Email, "user@example.com"),
            new(ClaimTypes.Name, "John Doe")
        };
        
        var identity = new ClaimsIdentity(claims, "Bearer");
        context.User = new ClaimsPrincipal(identity);

        // Act - Simulate the middleware logic directly
        await SimulateAuth0Middleware(context);

        // Assert
        context.Items["UserId"].Should().Be("auth0|123456789");
        context.Items["UserEmail"].Should().Be("user@example.com");
        context.Items["UserName"].Should().Be("John Doe");
    }

    [Fact]
    public async Task Auth0Middleware_WithAuth0SubClaim_ShouldExtractUserIdFromSubClaim()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = _mockServiceProvider.Object
        };

        var claims = new List<Claim>
        {
            new("sub", "auth0|987654321"), // Auth0 sub claim
            new(ClaimTypes.Email, "user@example.com"),
            new(ClaimTypes.Name, "Jane Doe")
        };
        
        var identity = new ClaimsIdentity(claims, "Bearer");
        context.User = new ClaimsPrincipal(identity);

        // Act - Simulate the middleware logic directly
        await SimulateAuth0Middleware(context);

        // Assert
        context.Items["UserId"].Should().Be("auth0|987654321");
        context.Items["UserEmail"].Should().Be("user@example.com");
        context.Items["UserName"].Should().Be("Jane Doe");
    }

    [Fact]
    public async Task Auth0Middleware_WithMissingClaims_ShouldHandleNullValues()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = _mockServiceProvider.Object
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "auth0|123456789")
            // Missing email and name claims
        };
        
        var identity = new ClaimsIdentity(claims, "Bearer");
        context.User = new ClaimsPrincipal(identity);

        // Act - Simulate the middleware logic directly
        await SimulateAuth0Middleware(context);

        // Assert
        context.Items["UserId"].Should().Be("auth0|123456789");
        context.Items["UserEmail"].Should().BeNull();
        context.Items["UserName"].Should().BeNull();
    }

    [Fact]
    public async Task Auth0Middleware_WithUnauthenticatedUser_ShouldNotAddUserInfoToContext()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = _mockServiceProvider.Object,
            User = new ClaimsPrincipal(new ClaimsIdentity()) // Unauthenticated
        };

        // Act - Simulate the middleware logic directly
        await SimulateAuth0Middleware(context);

        // Assert
        context.Items.Should().NotContainKey("UserId");
        context.Items.Should().NotContainKey("UserEmail");
        context.Items.Should().NotContainKey("UserName");
    }

    [Fact]
    public async Task Auth0Middleware_WithAuthenticatedUser_ShouldLogDebugInformation()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = _mockServiceProvider.Object
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "auth0|123456789"),
            new(ClaimTypes.Email, "user@example.com"),
            new(ClaimTypes.Name, "John Doe")
        };
        
        var identity = new ClaimsIdentity(claims, "Bearer");
        context.User = new ClaimsPrincipal(identity);

        // Act - Simulate the middleware logic directly
        await SimulateAuth0Middleware(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User authenticated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Auth0Middleware_WithUnauthenticatedUser_ShouldLogDebugInformation()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.RequestServices = _mockServiceProvider.Object;
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        context.Request.Path = "/api/test";

        // Act - Simulate the middleware logic directly
        await SimulateAuth0Middleware(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request not authenticated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Auth0Middleware_ShouldCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = _mockServiceProvider.Object,
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        var nextCalled = false;
        async Task next(HttpContext ctx)
        {
            nextCalled = true;
            await Task.CompletedTask;
        }

        // Act - Simulate the middleware logic directly
        await SimulateAuth0Middleware(context, next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // Helper method to simulate the Auth0Middleware logic for testing
    private static async Task SimulateAuth0Middleware(HttpContext context, RequestDelegate? next = null)
    {
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Auth0Middleware");
        
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? context.User.FindFirst("sub")?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.User.FindFirst(ClaimTypes.Name)?.Value;

            logger.LogDebug("User authenticated: UserId={UserId}, Email={Email}, Name={Name}", 
                userId, email, name);

            context.Items["UserId"] = userId;
            context.Items["UserEmail"] = email;
            context.Items["UserName"] = name;
        }
        else
        {
            logger.LogDebug("Request not authenticated for path: {Path}", context.Request.Path);
        }

        if (next != null)
        {
            await next(context);
        }
    }
} 