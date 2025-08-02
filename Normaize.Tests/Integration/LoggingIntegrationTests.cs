using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Normaize.Core.Interfaces;
using FluentAssertions;
using System.Net;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Normaize.Data;
using Moq;
using System.Security.Claims;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Normaize.Core.Services;

namespace Normaize.Tests.Integration;

public class LoggingIntegrationTests(TestWebApplicationFactory _factory) : IClassFixture<TestWebApplicationFactory>
{
    // Force static constructor to run by referencing a static readonly field
    private static readonly object _ = InitTestEnv();
    private static object InitTestEnv() { var _ = typeof(TestSetup); return null!; }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk_WhenApplicationIsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test mode to prevent ServiceConfiguration.ConfigureServices from being called
        Environment.SetEnvironmentVariable("TEST_MODE", "true");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        // Ensure environment variables are cleared before host is built
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQLPORT", null);
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
        Environment.SetEnvironmentVariable("SFTP_HOST", null);

        // Set test environment
        builder.UseEnvironment("Test");

        // Configure services for testing
        builder.ConfigureServices(services =>
        {
            // Add in-memory database
            services.AddDbContext<NormaizeContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));

            // Add required services for testing
            services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
            services.AddScoped<IUserSettingsService, UserSettingsService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
            services.AddScoped<IDataSetRepository, DataSetRepository>();
            services.AddScoped<IAnalysisRepository, AnalysisRepository>();
            services.AddScoped<IDataSetRowRepository, DataSetRowRepository>();
            services.AddScoped<IDataProcessingService, DataProcessingService>();
            services.AddScoped<IDataAnalysisService, DataAnalysisService>();
            services.AddScoped<IDataVisualizationService, DataVisualizationService>();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IMigrationService, MigrationService>();
            services.AddScoped<IHealthCheckService, HealthCheckService>();
            services.AddScoped<IStartupService, StartupService>();
            services.AddScoped<IStorageService, InMemoryStorageService>();
            services.AddScoped<IStorageConfigurationService, StorageConfigurationService>();
            services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();

            // Mock the IAppConfigurationService to return test environment
            var mockAppConfig = new Mock<IAppConfigurationService>();
            mockAppConfig.Setup(x => x.GetEnvironment()).Returns("Test");
            mockAppConfig.Setup(x => x.GetDatabaseConfig()).Returns(new DatabaseConfig());
            mockAppConfig.Setup(x => x.HasDatabaseConnection()).Returns(false);
            services.AddSingleton(mockAppConfig.Object);



            // Add HttpContextAccessor
            services.AddHttpContextAccessor();

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("TestPolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Add authentication (simplified for testing)
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

            services.AddAuthorization();

            // Add controllers
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Add health checks
            services.AddHealthChecks();

            // Add HttpClientFactory
            services.AddHttpClient();
        });
    }
}

// Test authentication scheme for integration tests
public class TestAuthenticationSchemeOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
{
}

public class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        // Create a test user for integration tests
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "auth0|123456789"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}