using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Data;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Services;

public class DatabaseHealthServiceTests
{
    private readonly Mock<ILogger<DatabaseHealthService>> _mockLogger = new();
    private readonly DatabaseHealthConfiguration _defaultConfig = new();
    private readonly Mock<IOptions<DatabaseHealthConfiguration>> _mockOptions;

    public DatabaseHealthServiceTests()
    {
        _mockOptions = new Mock<IOptions<DatabaseHealthConfiguration>>();
        _mockOptions.Setup(x => x.Value).Returns(_defaultConfig);
    }

    private static NormaizeContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NormaizeContext(options);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenInMemoryDb()
    {
        using var context = CreateInMemoryContext();
        var service = new DatabaseHealthService(context, _mockLogger.Object, _mockOptions.Object);
        var result = await service.CheckHealthAsync();
        Assert.True(result.IsHealthy);
        Assert.Equal("healthy", result.Status);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenCannotConnect()
    {
        // Use a context that will fail to connect
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseSqlite("Data Source=:memory:;Mode=ReadOnly") // Read-only mode will cause connection issues
            .Options;
        using var context = new NormaizeContext(options);
        var service = new DatabaseHealthService(context, _mockLogger.Object, _mockOptions.Object);
        var result = await service.CheckHealthAsync();
        Assert.False(result.IsHealthy);
        Assert.Equal("unhealthy", result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenMissingColumns()
    {
        // Use Sqlite and create DataSets table with missing columns
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseSqlite("Filename=:memory:")
            .Options;
        using var context = new NormaizeContext(options);
        context.Database.OpenConnection();
        context.Database.ExecuteSqlRaw("CREATE TABLE DataSets (Id INTEGER PRIMARY KEY)");
        var service = new DatabaseHealthService(context, _mockLogger.Object, _mockOptions.Object);
        var result = await service.CheckHealthAsync();
        Assert.False(result.IsHealthy);
        Assert.Equal("unhealthy", result.Status);
        Assert.Contains("Missing critical columns", result.ErrorMessage);
        Assert.NotEmpty(result.MissingColumns);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenAllColumnsPresent()
    {
        // Use Sqlite and create DataSets table with all critical columns
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseSqlite("Filename=:memory:")
            .Options;
        using var context = new NormaizeContext(options);
        context.Database.OpenConnection();
        context.Database.ExecuteSqlRaw(@"CREATE TABLE DataSets (
            Id INTEGER PRIMARY KEY,
            DataHash TEXT,
            UserId TEXT,
            FilePath TEXT,
            StorageProvider TEXT
        )");
        var service = new DatabaseHealthService(context, _mockLogger.Object, _mockOptions.Object);
        var result = await service.CheckHealthAsync();
        Assert.True(result.IsHealthy);
        Assert.Equal("healthy", result.Status);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(result.MissingColumns);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_OnException()
    {
        // Create a context that will throw an exception during health check
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseSqlite("Data Source=invalid_file.db") // Invalid file will cause exception
            .Options;
        using var context = new NormaizeContext(options);
        var service = new DatabaseHealthService(context, _mockLogger.Object, _mockOptions.Object);
        var result = await service.CheckHealthAsync();
        Assert.False(result.IsHealthy);
        Assert.Equal("unhealthy", result.Status);
        Assert.NotNull(result.ErrorMessage);
    }
} 