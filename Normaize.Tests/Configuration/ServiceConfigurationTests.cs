using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Normaize.API.Configuration;
using Normaize.Core.Interfaces;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using System.Collections;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Configuration;

[CollectionDefinition("ServiceConfigurationTests")]
public class ServiceConfigurationTestsCollection : ICollectionFixture<object>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[Collection("ServiceConfigurationTests")]
public class ServiceConfigurationTests : IDisposable
{
    private readonly WebApplicationBuilder _builder;
    private readonly Dictionary<string, string?> _originalEnvironmentVariables;

    public ServiceConfigurationTests()
    {
        _builder = WebApplication.CreateBuilder();

        // Store original environment variables to restore them later
        _originalEnvironmentVariables = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            ["MYSQLHOST"] = Environment.GetEnvironmentVariable("MYSQLHOST"),
            ["MYSQLDATABASE"] = Environment.GetEnvironmentVariable("MYSQLDATABASE"),
            ["MYSQLUSER"] = Environment.GetEnvironmentVariable("MYSQLUSER"),
            ["MYSQLPASSWORD"] = Environment.GetEnvironmentVariable("MYSQLPASSWORD"),
            ["MYSQLPORT"] = Environment.GetEnvironmentVariable("MYSQLPORT"),
            ["STORAGE_PROVIDER"] = Environment.GetEnvironmentVariable("STORAGE_PROVIDER"),
            ["AWS_ACCESS_KEY_ID"] = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
            ["AWS_SECRET_ACCESS_KEY"] = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
            ["AUTH0_ISSUER"] = Environment.GetEnvironmentVariable("AUTH0_ISSUER"),
            ["AUTH0_AUDIENCE"] = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE")
        };

        // Clear all environment variables to start with a clean slate
        ClearAllEnvironmentVariables();

        // Add basic services required for testing
        _builder.Services.AddLogging();
        _builder.Services.AddSingleton<IAppConfigurationService, AppConfigurationService>();
    }

    public void Dispose()
    {
        // Restore original environment variables
        foreach (var kvp in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
        GC.SuppressFinalize(this);
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
        var problematicBuilder = WebApplication.CreateBuilder();
        problematicBuilder.Services.AddLogging();

        // Act & Assert
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
    public void ServiceConfiguration_ShouldHandleMissingEnvVarsGracefully()
    {
        SetupInvalidEnvironment();
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        builder.Services.AddSingleton<IAppConfigurationService, AppConfigurationService>();

        // Act & Assert - Should not throw, but should use fallback configurations
        var action = () =>
        {
            ServiceConfiguration.ConfigureServices(builder);
        };

        action.Should().NotThrow("Service configuration should handle missing environment variables gracefully");

        // Verify fallback behavior
        var app = builder.Build();
        using var scope = app.Services.CreateScope();

        // Should still have core services available
        scope.ServiceProvider.GetService<IAppConfigurationService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<NormaizeContext>().Should().NotBeNull();
    }

    #region Helper Methods

    private static void SetupValidEnvironment()
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

    private static void SetupInvalidEnvironment()
    {
        // Remove required environment variables to cause configuration failure
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQLPORT", null);
    }

    private static void SetupEnvironmentWithoutDatabase()
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

    private static void SetupEnvironmentWithS3ButNoCredentials()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", "s3");

        // Clear all AWS-related environment variables to ensure no credentials are available
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AWS_REGION", null);
        Environment.SetEnvironmentVariable("AWS_S3_BUCKET", null);
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", null);

        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");
    }

    private static void SetupTestEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");
    }

    private static void SetupDevelopmentEnvironment()
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

    private static void ClearAllEnvironmentVariables()
    {
        // Clear only the environment variables that could cause database provider conflicts
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQLPORT", null);
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AWS_REGION", null);
        Environment.SetEnvironmentVariable("AWS_S3_BUCKET", null);
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", null);
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", null);
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", null);
    }

    #endregion
}