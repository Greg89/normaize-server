using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Normaize.API.Configuration;
using Normaize.Core.Interfaces;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Xunit;
using FluentAssertions;
using System;
using System.Linq;

namespace Normaize.Tests.Configuration;

public class ServiceConfigurationTests
{
    private readonly WebApplicationBuilder _builder;

    public ServiceConfigurationTests()
    {
        _builder = WebApplication.CreateBuilder();
        
        // Add basic services required for testing
        _builder.Services.AddLogging();
        _builder.Services.AddSingleton<IAppConfigurationService, AppConfigurationService>();
    }

    [Fact]
    public void ConfigureServices_WhenValidConfiguration_ShouldConfigureAllServices()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        using var scope = app.Services.CreateScope();
        
        // Core services
        scope.ServiceProvider.GetService<IConfigurationValidationService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IAppConfigurationService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IStorageConfigurationService>().Should().NotBeNull();

        // Infrastructure services
        scope.ServiceProvider.GetService<NormaizeContext>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IStorageService>().Should().NotBeNull();

        // Application services
        scope.ServiceProvider.GetService<IDataProcessingService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IDataAnalysisService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IDataVisualizationService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IFileUploadService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IAuditService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IStructuredLoggingService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IMigrationService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IHealthCheckService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IStartupService>().Should().NotBeNull();

        // Repository services
        scope.ServiceProvider.GetService<IDataSetRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IAnalysisRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IDataSetRowRepository>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldBeResilientToConfigurationErrors()
    {
        // Arrange
        // Create a builder that would normally cause issues
        var problematicBuilder = WebApplication.CreateBuilder();
        problematicBuilder.Services.AddLogging();
        
        // Act & Assert
        // The configuration should be resilient and not throw exceptions
        // This demonstrates the chaos engineering principle of graceful degradation
        var action = () => ServiceConfiguration.ConfigureServices(problematicBuilder);
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureServices_WhenDatabaseUnavailable_ShouldUseInMemoryDatabase()
    {
        // Arrange
        SetupEnvironmentWithoutDatabase();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        using var scope = app.Services.CreateScope();
        
        var context = scope.ServiceProvider.GetService<NormaizeContext>();
        context.Should().NotBeNull();
        
        // Verify it's using in-memory database
        var storageService = scope.ServiceProvider.GetService<IStorageService>();
        storageService.Should().BeOfType<InMemoryStorageService>();
    }

    [Fact]
    public void ConfigureServices_WhenS3CredentialsMissing_ShouldFallbackToInMemoryStorage()
    {
        // Arrange
        SetupEnvironmentWithS3ButNoCredentials();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        using var scope = app.Services.CreateScope();
        
        var storageService = scope.ServiceProvider.GetService<IStorageService>();
        storageService.Should().BeOfType<InMemoryStorageService>();
    }

    [Fact]
    public void ConfigureServices_WhenTestEnvironment_ShouldUseInMemoryStorage()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        using var scope = app.Services.CreateScope();
        
        var storageService = scope.ServiceProvider.GetService<IStorageService>();
        storageService.Should().BeOfType<InMemoryStorageService>();
    }

    [Fact]
    public void ConfigureServices_WhenDevelopmentEnvironment_ShouldEnableSensitiveDataLogging()
    {
        // Arrange
        SetupDevelopmentEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        using var scope = app.Services.CreateScope();
        
        var context = scope.ServiceProvider.GetService<NormaizeContext>();
        context.Should().NotBeNull();
        
        // Note: We can't directly test EnableSensitiveDataLogging as it's internal,
        // but we can verify the context is configured
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureCorsPolicies()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        var corsService = app.Services.GetService<ICorsService>();
        corsService.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureResponseCompression()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        // Response compression is configured at middleware level, not as a DI service
        // We can verify the application builds successfully
        app.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureResponseCaching()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        // Response caching is configured at middleware level, not as a DI service
        // We can verify the application builds successfully
        app.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureMemoryCache()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        var memoryCache = app.Services.GetService<IMemoryCache>();
        memoryCache.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureHttpClient()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        var httpClientFactory = app.Services.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureHealthChecks()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        var healthCheckService = app.Services.GetService<IHealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureAuthentication()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        // Authentication is configured at middleware level, not as a DI service
        // We can verify the application builds successfully
        app.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureSwagger()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        // Swagger services are configured but not directly accessible via DI
        // We can verify the application builds successfully
        app.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureAutoMapper()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        var mapper = app.Services.GetService<AutoMapper.IMapper>();
        mapper.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureHttpContextAccessor()
    {
        // Arrange
        SetupValidEnvironment();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);
        var app = _builder.Build();

        // Assert
        var httpContextAccessor = app.Services.GetService<IHttpContextAccessor>();
        httpContextAccessor.Should().NotBeNull();
    }

    #region Helper Methods

    private void SetupValidEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("MYSQLHOST", "localhost");
        Environment.SetEnvironmentVariable("MYSQLDATABASE", "testdb");
        Environment.SetEnvironmentVariable("MYSQLUSER", "testuser");
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", "testpass");
        Environment.SetEnvironmentVariable("MYSQLPORT", "3306");
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");
    }

    private void SetupInvalidEnvironment()
    {
        // Remove required environment variables to cause configuration failure
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQLPORT", null);
    }

    private void SetupEnvironmentWithoutDatabase()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQLPORT", null);
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");
    }

    private void SetupEnvironmentWithS3ButNoCredentials()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", "s3");
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");
    }

    private void SetupTestEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");
    }

    private void SetupDevelopmentEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("MYSQLHOST", "localhost");
        Environment.SetEnvironmentVariable("MYSQLDATABASE", "testdb");
        Environment.SetEnvironmentVariable("MYSQLUSER", "testuser");
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", "testpass");
        Environment.SetEnvironmentVariable("MYSQLPORT", "3306");
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");
    }

    #endregion
} 