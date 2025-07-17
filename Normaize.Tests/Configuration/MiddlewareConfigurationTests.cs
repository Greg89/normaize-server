using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Normaize.API.Configuration;
using Normaize.Core.Interfaces;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Configuration;

public class MiddlewareConfigurationTests
{
    [Fact]
    public void ConfigureMiddleware_ShouldNotThrow_WhenCalledWithValidApp()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Development");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services that middleware expects
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify logging was called
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ConfigureMiddleware_ShouldHandleNullAppConfigService_Gracefully()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        // Don't register IAppConfigurationService to simulate null scenario
        builder.Services.AddSingleton(mockLoggingService.Object);
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify logging was called
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Beta")]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Test")]
    public void ConfigureMiddleware_ShouldHandleAllEnvironments_Gracefully(string environment)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns(environment ?? "Development");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify logging was called
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ConfigureMiddleware_ShouldHandleInvalidEnvironmentValues_Gracefully(string? environment)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns(environment ?? "Development");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify logging was called
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("invalid-port")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("99999")]
    public void ConfigureMiddleware_ShouldHandleInvalidHttpsPort_Gracefully(string invalidPort)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Production");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns(invalidPort);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify logging was called
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("8443")]
    [InlineData("443")]
    [InlineData("3000")]
    public void ConfigureMiddleware_ShouldHandleValidHttpsPort_Gracefully(string validPort)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Production");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns(validPort);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify logging was called
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ConfigureMiddleware_ShouldHandleAppConfigServiceThrowingException_Gracefully()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Throws(new InvalidOperationException("Test exception"));
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify exception was logged
        mockLoggingService.Verify(x => x.LogException(It.IsAny<Exception>(), It.IsAny<string>()), Times.AtLeastOnce);
        // Verify configuration still completed
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ConfigureMiddleware_ShouldHandleAllServicesThrowingExceptions_Gracefully()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Throws(new InvalidOperationException("Environment exception"));
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Throws(new InvalidOperationException("HTTPS port exception"));
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify exceptions were logged
        mockLoggingService.Verify(x => x.LogException(It.IsAny<Exception>(), It.IsAny<string>()), Times.AtLeastOnce);
        // Verify configuration still completed
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ConfigureMiddleware_ShouldCompleteSuccessfully_WithAllRequiredServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Development");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
        
        // Verify logging was called
        mockLoggingService.Verify(x => x.LogUserAction(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ConfigureMiddleware_ShouldLogConfigurationDecisions()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        var mockLoggingService = new Mock<IStructuredLoggingService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Development");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns("8443");
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        builder.Services.AddSingleton(mockLoggingService.Object);
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();
        
        // Set the environment to Development so Swagger gets configured
        app.Environment.EnvironmentName = "Development";

        // Act
        MiddlewareConfiguration.ConfigureMiddleware(app);

        // Assert - Verify specific configuration decisions were logged
        mockLoggingService.Verify(x => x.LogUserAction("Middleware configuration started", It.IsAny<object>()), Times.Once);
        mockLoggingService.Verify(x => x.LogUserAction("Middleware configuration completed successfully", It.IsAny<object>()), Times.Once);
        mockLoggingService.Verify(x => x.LogUserAction("Swagger configured", It.IsAny<object>()), Times.Once);
        mockLoggingService.Verify(x => x.LogUserAction("CORS configured with AllowAll policy", It.IsAny<object>()), Times.Once);
        mockLoggingService.Verify(x => x.LogUserAction("HTTPS redirection configured for development", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public void ConfigureMiddleware_ShouldHandleNullLoggingService_Gracefully()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Development");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
        
        builder.Services.AddSingleton(mockAppConfigService.Object);
        // Don't register IStructuredLoggingService to simulate null scenario
        
        // Add all required services
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy("Restrictive", policy => policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();

        // Act & Assert
        var action = () => MiddlewareConfiguration.ConfigureMiddleware(app);
        action.Should().NotThrow();
    }
} 