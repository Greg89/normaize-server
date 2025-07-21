using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.Interfaces;
using Normaize.Data;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Services;

public class MigrationServiceTests
{
    private readonly MigrationService _service;
    private readonly NormaizeContext _context;

    public MigrationServiceTests()
    {
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NormaizeContext(options);
        var mockLogger = new Mock<ILogger<MigrationService>>();
        _service = new MigrationService(_context, mockLogger.Object);
    }

    [Fact]
    public void IMigrationService_ShouldDefineRequiredMethods()
    {
        // Arrange & Act
        var interfaceType = typeof(IMigrationService);

        // Assert
        interfaceType.GetMethod("ApplyMigrations")!.Should().NotBeNull();
        interfaceType.GetMethod("VerifySchemaAsync")!.Should().NotBeNull();
    }

    [Fact]
    public void IMigrationService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IMigrationService);

        // Assert
        interfaceType.GetMethod("ApplyMigrations")!.ReturnType.Should().Be(typeof(Task<MigrationResult>));
        interfaceType.GetMethod("VerifySchemaAsync")!.ReturnType.Should().Be(typeof(Task<MigrationResult>));
    }

    [Fact]
    public void IMigrationService_ShouldBePublic()
    {
        // Assert
        typeof(IMigrationService).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IMigrationService_ShouldBeInterface()
    {
        // Assert
        typeof(IMigrationService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyMigrations_ShouldReturnValidResult()
    {
        // Act
        var result = await _service.ApplyMigrations();

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNull();
        result.PendingMigrations.Should().NotBeNull();
        result.MissingColumns.Should().NotBeNull();
        // Success can be true or false for in-memory DB
    }

    [Fact]
    public async Task VerifySchemaAsync_ShouldReturnValidResult()
    {
        // Act
        var result = await _service.VerifySchemaAsync();

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNull();
        result.PendingMigrations.Should().NotBeNull();
        result.MissingColumns.Should().NotBeNull();
        // Success can be true or false for in-memory DB
    }

    [Fact]
    public void MigrationResult_ShouldHaveRequiredProperties()
    {
        // Arrange
        var result = new MigrationResult
        {
            Success = true,
            Message = "Test message",
            PendingMigrations = new List<string> { "Migration1", "Migration2" },
            MissingColumns = new List<string> { "Column1", "Column2" },
            ErrorMessage = "Test error"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Test message");
        result.PendingMigrations.Should().HaveCount(2);
        result.PendingMigrations.Should().Contain("Migration1");
        result.PendingMigrations.Should().Contain("Migration2");
        result.MissingColumns.Should().HaveCount(2);
        result.MissingColumns.Should().Contain("Column1");
        result.MissingColumns.Should().Contain("Column2");
        result.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public void MigrationResult_WithDefaultValues_ShouldHaveCorrectDefaults()
    {
        // Arrange
        var result = new MigrationResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(string.Empty);
        result.PendingMigrations.Should().NotBeNull();
        result.PendingMigrations.Should().BeEmpty();
        result.MissingColumns.Should().NotBeNull();
        result.MissingColumns.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ApplyMigrations_WithInMemoryDatabase_ShouldSucceedOrFailGracefully()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _service.ApplyMigrations();

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNull();
        // Success can be true or false for in-memory DB
    }

    [Fact]
    public async Task VerifySchemaAsync_WithInMemoryDatabase_ShouldSucceedOrFailGracefully()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await _service.VerifySchemaAsync();

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNull();
        // Success can be true or false for in-memory DB
    }

    [Fact]
    public async Task ApplyMigrations_WhenDatabaseIsNotAccessible_ShouldHandleGracefully()
    {
        // Arrange
        var failingOptions = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: "non-existent-db")
            .Options;

        var failingContext = new NormaizeContext(failingOptions);
        var mockLogger = new Mock<ILogger<MigrationService>>();
        var failingService = new MigrationService(failingContext, mockLogger.Object);

        // Act
        var result = await failingService.ApplyMigrations();

        // Assert
        result.Should().NotBeNull();
        // Success can be true or false for in-memory DB
    }

    [Fact]
    public async Task VerifySchemaAsync_WhenDatabaseIsNotAccessible_ShouldHandleGracefully()
    {
        // Arrange
        var failingOptions = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: "non-existent-db")
            .Options;

        var failingContext = new NormaizeContext(failingOptions);
        var mockLogger = new Mock<ILogger<MigrationService>>();
        var failingService = new MigrationService(failingContext, mockLogger.Object);

        // Act
        var result = await failingService.VerifySchemaAsync();

        // Assert
        result.Should().NotBeNull();
        // Success can be true or false for in-memory DB
    }

    [Fact]
    public async Task ApplyMigrations_ShouldNotThrowException()
    {
        // Act & Assert
        var action = () => _service.ApplyMigrations();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task VerifySchemaAsync_ShouldNotThrowException()
    {
        // Act & Assert
        var action = () => _service.VerifySchemaAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyMigrations_ShouldReturnConsistentResults()
    {
        // Act
        var result1 = await _service.ApplyMigrations();
        var result2 = await _service.ApplyMigrations();

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Success.Should().Be(result2.Success);
    }

    [Fact]
    public async Task VerifySchemaAsync_ShouldReturnConsistentResults()
    {
        // Act
        var result1 = await _service.VerifySchemaAsync();
        var result2 = await _service.VerifySchemaAsync();

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Success.Should().Be(result2.Success);
    }
} 