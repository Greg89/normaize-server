using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data;
using Normaize.Data.Services;
using Xunit;

namespace Normaize.Tests.Services;

public class AuditServiceTests
{
    private readonly AuditService _service;
    private readonly NormaizeContext _context;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NormaizeContext(options);
        _service = new AuditService(_context);
    }

    [Fact]
    public void IAuditService_ShouldDefineRequiredMethods()
    {
        // Arrange & Act
        var interfaceType = typeof(IAuditService);

        // Assert
        interfaceType.GetMethod("LogDataSetActionAsync", new[] { typeof(int), typeof(string), typeof(string), typeof(object), typeof(string), typeof(string) })!.Should().NotBeNull();
        interfaceType.GetMethod("GetDataSetAuditLogsAsync", new[] { typeof(int), typeof(int), typeof(int) })!.Should().NotBeNull();
        interfaceType.GetMethod("GetUserAuditLogsAsync", new[] { typeof(string), typeof(int), typeof(int) })!.Should().NotBeNull();
        interfaceType.GetMethod("GetAuditLogsByActionAsync", new[] { typeof(string), typeof(int), typeof(int) })!.Should().NotBeNull();
    }

    [Fact]
    public void IAuditService_MethodsShouldReturnCorrectTypes()
    {
        // Arrange
        var interfaceType = typeof(IAuditService);

        // Assert
        interfaceType.GetMethod("LogDataSetActionAsync", [typeof(int), typeof(string), typeof(string), typeof(object), typeof(string), typeof(string)])!.ReturnType.Should().Be<Task>();
        interfaceType.GetMethod("GetDataSetAuditLogsAsync", [typeof(int), typeof(int), typeof(int)])!.ReturnType.Should().Be<Task<IEnumerable<DataSetAuditLog>>>();
        interfaceType.GetMethod("GetUserAuditLogsAsync", [typeof(string), typeof(int), typeof(int)])!.ReturnType.Should().Be<Task<IEnumerable<DataSetAuditLog>>>();
        interfaceType.GetMethod("GetAuditLogsByActionAsync", [typeof(string), typeof(int), typeof(int)])!.ReturnType.Should().Be<Task<IEnumerable<DataSetAuditLog>>>();
    }

    [Fact]
    public void IAuditService_ShouldBePublic()
    {
        // Assert
        typeof(IAuditService).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void IAuditService_ShouldBeInterface()
    {
        // Assert
        typeof(IAuditService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public async Task LogDataSetActionAsync_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var action = "CREATE";
        var changes = new { field = "value" };
        var ipAddress = "127.0.0.1";
        var userAgent = "test-agent";

        // Act & Assert
        var actionToTest = () => _service.LogDataSetActionAsync(dataSetId, userId, action, changes, ipAddress, userAgent);
        await actionToTest.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_WithNullChanges_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var action = "CREATE";

        // Act & Assert
        var actionToTest = () => _service.LogDataSetActionAsync(dataSetId, userId, action, null);
        await actionToTest.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_WithNullIpAddress_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var action = "CREATE";

        // Act & Assert
        var actionToTest = () => _service.LogDataSetActionAsync(dataSetId, userId, action, null, null);
        await actionToTest.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_WithNullUserAgent_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var action = "CREATE";

        // Act & Assert
        var actionToTest = () => _service.LogDataSetActionAsync(dataSetId, userId, action, null, "127.0.0.1", null);
        await actionToTest.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDataSetAuditLogsAsync_WithValidParameters_ShouldReturnEmptyCollection()
    {
        // Arrange
        var dataSetId = 1;
        var skip = 0;
        var take = 50;

        // Act
        var result = await _service.GetDataSetAuditLogsAsync(dataSetId, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDataSetAuditLogsAsync_WithDefaultParameters_ShouldReturnEmptyCollection()
    {
        // Arrange
        var dataSetId = 1;

        // Act
        var result = await _service.GetDataSetAuditLogsAsync(dataSetId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDataSetAuditLogsAsync_WithPagination_ShouldReturnEmptyCollection()
    {
        // Arrange
        var dataSetId = 1;
        var skip = 10;
        var take = 20;

        // Act
        var result = await _service.GetDataSetAuditLogsAsync(dataSetId, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_WithValidParameters_ShouldReturnEmptyCollection()
    {
        // Arrange
        var userId = "test-user";
        var skip = 0;
        var take = 50;

        // Act
        var result = await _service.GetUserAuditLogsAsync(userId, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_WithDefaultParameters_ShouldReturnEmptyCollection()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _service.GetUserAuditLogsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_WithPagination_ShouldReturnEmptyCollection()
    {
        // Arrange
        var userId = "test-user";
        var skip = 10;
        var take = 20;

        // Act
        var result = await _service.GetUserAuditLogsAsync(userId, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogsByActionAsync_WithValidParameters_ShouldReturnEmptyCollection()
    {
        // Arrange
        var action = "CREATE";
        var skip = 0;
        var take = 50;

        // Act
        var result = await _service.GetAuditLogsByActionAsync(action, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogsByActionAsync_WithDefaultParameters_ShouldReturnEmptyCollection()
    {
        // Arrange
        var action = "CREATE";

        // Act
        var result = await _service.GetAuditLogsByActionAsync(action);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogsByActionAsync_WithPagination_ShouldReturnEmptyCollection()
    {
        // Arrange
        var action = "CREATE";
        var skip = 10;
        var take = 20;

        // Act
        var result = await _service.GetAuditLogsByActionAsync(action, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LogDataSetActionAsync_WithNegativeDataSetId_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = -1;
        var userId = "test-user";
        var action = "CREATE";

        // Act & Assert
        var actionToTest = () => _service.LogDataSetActionAsync(dataSetId, userId, action);
        await actionToTest.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_WithEmptyUserId_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "";
        var action = "CREATE";

        // Act & Assert
        var actionToTest = () => _service.LogDataSetActionAsync(dataSetId, userId, action);
        await actionToTest.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_WithEmptyAction_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var action = "";

        // Act & Assert
        var actionToTest = () => _service.LogDataSetActionAsync(dataSetId, userId, action);
        await actionToTest.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDataSetAuditLogsAsync_WithNegativeDataSetId_ShouldReturnEmptyCollection()
    {
        // Arrange
        var dataSetId = -1;

        // Act
        var result = await _service.GetDataSetAuditLogsAsync(dataSetId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_WithEmptyUserId_ShouldReturnEmptyCollection()
    {
        // Arrange
        var userId = "";

        // Act
        var result = await _service.GetUserAuditLogsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogsByActionAsync_WithEmptyAction_ShouldReturnEmptyCollection()
    {
        // Arrange
        var action = "";

        // Act
        var result = await _service.GetAuditLogsByActionAsync(action);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDataSetAuditLogsAsync_WithNegativeSkip_ShouldReturnEmptyCollection()
    {
        // Arrange
        var dataSetId = 1;
        var skip = -10;
        var take = 50;

        // Act
        var result = await _service.GetDataSetAuditLogsAsync(dataSetId, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDataSetAuditLogsAsync_WithZeroTake_ShouldReturnEmptyCollection()
    {
        // Arrange
        var dataSetId = 1;
        var skip = 0;
        var take = 0;

        // Act
        var result = await _service.GetDataSetAuditLogsAsync(dataSetId, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDataSetAuditLogsAsync_WithNegativeTake_ShouldReturnEmptyCollection()
    {
        // Arrange
        var dataSetId = 1;
        var skip = 0;
        var take = -10;

        // Act
        var result = await _service.GetDataSetAuditLogsAsync(dataSetId, skip, take);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
} 