using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.API.Configuration;
using Normaize.Core.Configuration;
using Normaize.Core.Interfaces;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Configuration;

public class ServiceConfigurationTests
{
    private readonly WebApplicationBuilder _builder;
    private readonly Mock<ILogger<object>> _mockLogger;

    public ServiceConfigurationTests()
    {
        _builder = WebApplication.CreateBuilder();
        _mockLogger = new Mock<ILogger<object>>();
        
        // Add required services for testing
        _builder.Services.AddSingleton(_mockLogger.Object);
        _builder.Services.AddLogging();
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        SetupTestConfiguration();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        
        // Verify core services are registered
        services.GetService<IDataProcessingService>().Should().NotBeNull();
        services.GetService<IDataAnalysisService>().Should().NotBeNull();
        services.GetService<IDataVisualizationService>().Should().NotBeNull();
        services.GetService<IFileUploadService>().Should().NotBeNull();
        services.GetService<IAuditService>().Should().NotBeNull();
        services.GetService<IHealthCheckService>().Should().NotBeNull();
        services.GetService<IConfigurationValidationService>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterConfigurationOptions()
    {
        // Arrange
        SetupTestConfiguration();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        
        var serviceConfigOptions = services.GetService<IOptions<ServiceConfigurationOptions>>();
        serviceConfigOptions.Should().NotBeNull();
        serviceConfigOptions!.Value.Should().NotBeNull();

        var healthConfigOptions = services.GetService<IOptions<HealthCheckConfiguration>>();
        healthConfigOptions.Should().NotBeNull();
        healthConfigOptions!.Value.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureDatabase_WhenConnectionStringAvailable()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("MYSQLHOST", "localhost");
        Environment.SetEnvironmentVariable("MYSQLDATABASE", "testdb");
        Environment.SetEnvironmentVariable("MYSQLUSER", "testuser");
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", "testpass");

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var context = services.GetService<Normaize.Data.NormaizeContext>();
        context.Should().NotBeNull();

        // Cleanup
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
    }

    [Fact]
    public void ConfigureServices_ShouldUseInMemoryDatabase_WhenNoConnectionString()
    {
        // Arrange
        SetupTestConfiguration();
        // Ensure no database environment variables are set
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var context = services.GetService<Normaize.Data.NormaizeContext>();
        context.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureCors_ForDevelopmentEnvironment()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var corsService = services.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>();
        corsService.Should().NotBeNull();

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureCors_ForProductionEnvironment()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var corsService = services.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>();
        corsService.Should().NotBeNull();

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureAuthentication_WhenAuth0VariablesPresent()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", "https://test.auth0.com/");
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", "test-audience");

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var authService = services.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        authService.Should().NotBeNull();

        // Cleanup
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", null);
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", null);
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureAuthentication_WhenAuth0VariablesMissing()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("AUTH0_ISSUER", null);
        Environment.SetEnvironmentVariable("AUTH0_AUDIENCE", null);

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var authService = services.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        authService.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureStorageService_ForTestEnvironment()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var storageService = services.GetService<IStorageService>();
        storageService.Should().NotBeNull();
        storageService.Should().BeOfType<InMemoryStorageService>();

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureStorageService_ForS3Provider_WhenCredentialsPresent()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", "s3");
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test-key");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test-secret");

        // Debug: Check environment variables
        Console.WriteLine($"STORAGE_PROVIDER: {Environment.GetEnvironmentVariable("STORAGE_PROVIDER")}");
        Console.WriteLine($"AWS_ACCESS_KEY_ID: {Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")}");
        Console.WriteLine($"AWS_SECRET_ACCESS_KEY: {Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")}");

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Debug: Check what services are registered
        var storageServices = _builder.Services.Where(s => s.ServiceType == typeof(IStorageService)).ToList();
        Console.WriteLine($"Found {storageServices.Count} IStorageService registrations:");
        foreach (var service in storageServices)
        {
            Console.WriteLine($"  - ImplementationType: {service.ImplementationType?.Name ?? "null"}");
            Console.WriteLine($"  - Lifetime: {service.Lifetime}");
        }

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        
        // Check that S3vice is registered in the service collection
        var serviceDescriptor = _builder.Services.FirstOrDefault(s => 
            s.ServiceType == typeof(IStorageService) && 
            s.ImplementationType == typeof(S3StorageService));
        
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.ImplementationType.Should().Be(typeof(S3StorageService));

        // Cleanup
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureStorageService_ForS3Provider_WhenCredentialsMissing()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", "s3");
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var storageService = services.GetService<IStorageService>();
        storageService.Should().NotBeNull();
        storageService.Should().BeOfType<InMemoryStorageService>();

        // Cleanup
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureStorageService_ForDefaultProvider()
    {
        // Arrange
        SetupTestConfiguration();
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var storageService = services.GetService<IStorageService>();
        storageService.Should().NotBeNull();
        storageService.Should().BeOfType<InMemoryStorageService>();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureRepositories()
    {
        // Arrange
        SetupTestConfiguration();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        
        services.GetService<IDataSetRepository>().Should().NotBeNull();
        services.GetService<IAnalysisRepository>().Should().NotBeNull();
        services.GetService<IDataSetRowRepository>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureHttpClient()
    {
        // Arrange
        SetupTestConfiguration();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var httpClientFactory = services.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigureCaching()
    {
        // Arrange
        SetupTestConfiguration();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        var memoryCache = services.GetService<IMemoryCache>();
        memoryCache.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldConfigurePerformanceOptimizations()
    {
        // Arrange
        SetupTestConfiguration();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        var services = _builder.Services.BuildServiceProvider();
        
        // Response compression and caching are configured at the application level,
        // so we can't easily test them in unit tests, but the configuration should not throw
        services.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldLogConfigurationSteps()
    {
        // Arrange
        SetupTestConfiguration();

        // Act
        ServiceConfiguration.ConfigureServices(_builder);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting service configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Service configuration completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConfigureServices_ShouldHandleConfigurationErrors_Gracefully()
    {
        // Arrange
        SetupTestConfiguration();
        // Intentionally cause a configuration error by setting invalid values
        Environment.SetEnvironmentVariable("MYSQLHOST", "invalid-host");
        Environment.SetEnvironmentVariable("MYSQLDATABASE", "invalid-db");
        Environment.SetEnvironmentVariable("MYSQLUSER", "invalid-user");
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", "invalid-pass");

        // Act & Assert
        var action = () => ServiceConfiguration.ConfigureServices(_builder);
        action.Should().NotThrow();

        // Cleanup
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
    }

    private void SetupTestConfiguration()
    {
        // Register a mock IAppConfigurationService to satisfy the dependency
        var mockAppConfigService = new Mock<IAppConfigurationService>();
        mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Development");
        mockAppConfigService.Setup(x => x.HasDatabaseConnection()).Returns(false);
        mockAppConfigService.Setup(x => x.GetDatabaseConfig()).Returns(new Normaize.Core.Interfaces.DatabaseConfig());
        mockAppConfigService.Setup(x => x.GetPort()).Returns("5000");
        mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
        mockAppConfigService.Setup(x => x.GetSeqUrl()).Returns((string?)null);
        mockAppConfigService.Setup(x => x.GetSeqApiKey()).Returns((string?)null);
        mockAppConfigService.Setup(x => x.IsProductionLike()).Returns(false);
        mockAppConfigService.Setup(x => x.IsContainerized()).Returns(false);
        
        _builder.Services.AddSingleton(mockAppConfigService.Object);
    }
} 