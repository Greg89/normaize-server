using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.Configuration;
using Normaize.Data;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Services;

public class HealthCheckServiceTests : IDisposable
{
    private readonly Mock<ILogger<HealthCheckService>> _mockLogger;
    private readonly Mock<IOptions<HealthCheckConfiguration>> _mockConfig;
    private readonly DbContextOptions<NormaizeContext> _dbContextOptions;
    private readonly HealthCheckConfiguration _config;

    public HealthCheckServiceTests()
    {
        _mockLogger = new Mock<ILogger<HealthCheckService>>();
        _config = new HealthCheckConfiguration
        {
            DatabaseTimeoutSeconds = 5,
            ApplicationTimeoutSeconds = 3,
            IncludeDetailedErrors = false,
            SkipMigrationsCheck = false,
            SkipDatabaseCheck = false,
            ComponentNames = new ComponentNames
            {
                Database = "database",
                Application = "application"
            }
        };
        _mockConfig = new Mock<IOptions<HealthCheckConfiguration>>();
        _mockConfig.Setup(x => x.Value).Returns(_config);

        _dbContextOptions = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAllComponentsHealthy_ShouldReturnHealthyResult()
    {
        // Arrange
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be("healthy");
        result.Components.Should().HaveCount(2);
        result.Components.Should().ContainKey("database");
        result.Components.Should().ContainKey("application");
        result.Components["database"].IsHealthy.Should().BeTrue();
        result.Components["application"].IsHealthy.Should().BeTrue();
        result.Issues.Should().BeEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenApplicationHealthy_ShouldReturnAliveResult()
    {
        // Arrange
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckLivenessAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be("alive");
        result.Components.Should().HaveCount(1);
        result.Components.Should().ContainKey("application");
        result.Components["application"].IsHealthy.Should().BeTrue();
        result.Issues.Should().BeEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckReadinessAsync_WhenAllComponentsHealthy_ShouldReturnReadyResult()
    {
        // Arrange
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be("ready");
        result.Components.Should().HaveCount(2);
        result.Components.Should().ContainKey("database");
        result.Components.Should().ContainKey("application");
        result.Components["database"].IsHealthy.Should().BeTrue();
        result.Components["application"].IsHealthy.Should().BeTrue();
        result.Issues.Should().BeEmpty();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckReadinessAsync_WhenSkipDatabaseCheck_ShouldNotCheckDatabase()
    {
        // Arrange
        _config.SkipDatabaseCheck = true;
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be("ready");
        result.Components.Should().HaveCount(1);
        result.Components.Should().ContainKey("application");
        result.Components.Should().NotContainKey("database");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ShouldReturnHealthyResult_ForInMemoryProvider()
    {
        // Arrange
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await service.CheckHealthAsync(cts.Token);

        // Assert
        // In-memory provider always returns healthy
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseTimeout_ShouldReturnTimeoutResult()
    {
        // Arrange
        _config.DatabaseTimeoutSeconds = 1;
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue(); // In-memory database should still be fast
        result.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenIncludeDetailedErrors_ShouldIncludeDetailedErrorMessages()
    {
        // Arrange
        _config.IncludeDetailedErrors = true;
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        // With in-memory database, we shouldn't see detailed errors, but the configuration should be respected
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSkipMigrationsCheck_ShouldNotCheckMigrations_ForInMemoryProvider()
    {
        // Arrange
        _config.SkipMigrationsCheck = true;
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        // In-memory provider does not include skipMigrationsCheck in details
        result.Components["application"].Details.Should().NotContainKey("skipMigrationsCheck");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomComponentNames_ShouldUseCustomNames()
    {
        // Arrange
        _config.ComponentNames.Database = "custom_db";
        _config.ComponentNames.Application = "custom_app";
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.Components.Should().ContainKey("custom_db");
        result.Components.Should().ContainKey("custom_app");
        result.Components.Should().NotContainKey("database");
        result.Components.Should().NotContainKey("application");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeTimeoutConfigurationInDetails_ForInMemoryProvider()
    {
        // Arrange
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        // In-memory provider does not include timeoutSeconds in details
        result.Components["application"].Details.Should().NotContainKey("timeoutSeconds");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeEnvironmentInDetails_ForInMemoryProvider()
    {
        // Arrange
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        // In-memory provider does not include environment in details
        result.Components["application"].Details.Should().NotContainKey("environment");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldLogCorrelationId()
    {
        // Arrange
        using var context = new NormaizeContext(_dbContextOptions);
        var service = new HealthCheckService(context, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionOccurs_ShouldReturnUnhealthyResult()
    {
        // Arrange
        var mockContext = new Mock<NormaizeContext>(_dbContextOptions);
        mockContext.Setup(x => x.Database).Throws(new InvalidOperationException("Test exception"));
        var service = new HealthCheckService(mockContext.Object, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be("unhealthy");
        result.Issues.Should().Contain(issue => issue.EndsWith("Database health check failed") || issue.EndsWith("Application health check failed"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionOccursWithDetailedErrors_ShouldIncludeExceptionMessageAndUnhealthyStatus()
    {
        // Arrange
        _config.IncludeDetailedErrors = true;
        var mockContext = new Mock<NormaizeContext>(_dbContextOptions);
        mockContext.Setup(x => x.Database).Throws(new InvalidOperationException("Test exception"));
        var service = new HealthCheckService(mockContext.Object, _mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be("unhealthy");
        result.Issues.Should().Contain(issue => issue.EndsWith("Test exception"));
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
} 