using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using FluentAssertions;
using Xunit;
using System.Security.Claims;

namespace Normaize.Tests.Controllers;

public class AuditControllerTests
{
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly AuditController _controller;
    private readonly DefaultHttpContext _httpContext;

    public AuditControllerTests()
    {
        _mockAuditService = new Mock<IAuditService>();
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _controller = new AuditController(_mockAuditService.Object, _mockLoggingService.Object);

        // Set up HTTP context with authentication
        _httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    private void SetupAuthenticatedUser(string userId = "test-user-123")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;
    }

    private void SetupUnauthenticatedUser()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
    }

    [Fact]
    public void Constructor_WithValidServices_ShouldCreateController()
    {
        // Act & Assert
        _controller.Should().NotBeNull();
        _controller.Should().BeOfType<AuditController>();
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAttribute()
    {
        // Act
        var authorizeAttribute = typeof(AuditController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .FirstOrDefault();

        // Assert
        authorizeAttribute.Should().NotBeNull();
    }

    [Fact]
    public void Controller_ShouldHaveApiControllerAttribute()
    {
        // Act
        var apiControllerAttribute = typeof(AuditController)
            .GetCustomAttributes(typeof(ApiControllerAttribute), true)
            .FirstOrDefault();

        // Assert
        apiControllerAttribute.Should().NotBeNull();
    }

    [Fact]
    public void Controller_ShouldHaveRouteAttribute()
    {
        // Act
        var routeAttribute = typeof(AuditController)
            .GetCustomAttributes(typeof(RouteAttribute), true)
            .FirstOrDefault();

        // Assert
        routeAttribute.Should().NotBeNull();
        var route = routeAttribute as RouteAttribute;
        route!.Template.Should().Be("api/[controller]");
    }

    #region GetDataSetAuditLogs Tests

    [Fact]
    public async Task GetDataSetAuditLogs_WithValidParameters_ShouldReturnOkResult()
    {
        // Arrange
        SetupAuthenticatedUser();
        var dataSetId = 1;
        var skip = 0;
        var take = 50;
        var expectedAuditLogs = new List<DataSetAuditLog>
        {
            new() { Id = 1, DataSetId = dataSetId, UserId = "user1", Action = "Created", Timestamp = DateTime.UtcNow },
            new() { Id = 2, DataSetId = dataSetId, UserId = "user2", Action = "Updated", Timestamp = DateTime.UtcNow }
        };

        _mockAuditService
            .Setup(x => x.GetDataSetAuditLogsAsync(dataSetId, skip, take))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetDataSetAuditLogs(dataSetId, skip, take);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetDataSetAuditLogsAsync(dataSetId, skip, take), Times.Once);
    }

    [Fact]
    public async Task GetDataSetAuditLogs_WithDefaultParameters_ShouldUseDefaultValues()
    {
        // Arrange
        SetupAuthenticatedUser();
        var dataSetId = 1;
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetDataSetAuditLogsAsync(dataSetId, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetDataSetAuditLogs(dataSetId);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetDataSetAuditLogsAsync(dataSetId, 0, 50), Times.Once);
    }

    [Fact]
    public async Task GetDataSetAuditLogs_WithCustomPagination_ShouldUseProvidedValues()
    {
        // Arrange
        SetupAuthenticatedUser();
        var dataSetId = 1;
        var skip = 10;
        var take = 25;
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetDataSetAuditLogsAsync(dataSetId, skip, take))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetDataSetAuditLogs(dataSetId, skip, take);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetDataSetAuditLogsAsync(dataSetId, skip, take), Times.Once);
    }

    [Fact]
    public async Task GetDataSetAuditLogs_WhenServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        SetupAuthenticatedUser();
        var dataSetId = 1;
        var exception = new InvalidOperationException("Database connection failed");

        _mockAuditService
            .Setup(x => x.GetDataSetAuditLogsAsync(dataSetId, 0, 50))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSetAuditLogs(dataSetId);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(400);
        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetDataSetAuditLogs_WithNegativeSkip_ShouldPassThroughToService(int skip)
    {
        // Arrange
        SetupAuthenticatedUser();
        var dataSetId = 1;
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetDataSetAuditLogsAsync(dataSetId, skip, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetDataSetAuditLogs(dataSetId, skip, 50);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual values passed
        _mockAuditService.Verify(x => x.GetDataSetAuditLogsAsync(dataSetId, skip, 50), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(200)]
    public async Task GetDataSetAuditLogs_WithInvalidTake_ShouldPassThroughToService(int take)
    {
        // Arrange
        SetupAuthenticatedUser();
        var dataSetId = 1;
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetDataSetAuditLogsAsync(dataSetId, 0, take))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetDataSetAuditLogs(dataSetId, 0, take);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual values passed
        _mockAuditService.Verify(x => x.GetDataSetAuditLogsAsync(dataSetId, 0, take), Times.Once);
    }

    #endregion

    #region GetUserAuditLogs Tests

    [Fact]
    public async Task GetUserAuditLogs_WithValidParameters_ShouldReturnOkResult()
    {
        // Arrange
        var userId = "test-user-123";
        SetupAuthenticatedUser(userId);
        var skip = 0;
        var take = 50;
        var expectedAuditLogs = new List<DataSetAuditLog>
        {
            new() { Id = 1, DataSetId = 1, UserId = userId, Action = "Created", Timestamp = DateTime.UtcNow },
            new() { Id = 2, DataSetId = 2, UserId = userId, Action = "Updated", Timestamp = DateTime.UtcNow }
        };

        _mockAuditService
            .Setup(x => x.GetUserAuditLogsAsync(userId, skip, take))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetUserAuditLogs(skip, take);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetUserAuditLogsAsync(userId, skip, take), Times.Once);
    }

    [Fact]
    public async Task GetUserAuditLogs_WithDefaultParameters_ShouldUseDefaultValues()
    {
        // Arrange
        var userId = "test-user-123";
        SetupAuthenticatedUser(userId);
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetUserAuditLogsAsync(userId, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetUserAuditLogs();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetUserAuditLogsAsync(userId, 0, 50), Times.Once);
    }

    [Fact]
    public async Task GetUserAuditLogs_WhenUserNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetUserAuditLogs();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(401);
        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserAuditLogs_WhenServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        var userId = "test-user-123";
        SetupAuthenticatedUser(userId);
        var exception = new InvalidOperationException("Database connection failed");

        _mockAuditService
            .Setup(x => x.GetUserAuditLogsAsync(userId, 0, 50))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetUserAuditLogs();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(400);
        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetUserAuditLogs_WithNegativeSkip_ShouldPassThroughToService(int skip)
    {
        // Arrange
        var userId = "test-user-123";
        SetupAuthenticatedUser(userId);
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetUserAuditLogsAsync(userId, skip, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetUserAuditLogs(skip, 50);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual values passed
        _mockAuditService.Verify(x => x.GetUserAuditLogsAsync(userId, skip, 50), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(200)]
    public async Task GetUserAuditLogs_WithInvalidTake_ShouldPassThroughToService(int take)
    {
        // Arrange
        var userId = "test-user-123";
        SetupAuthenticatedUser(userId);
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetUserAuditLogsAsync(userId, 0, take))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetUserAuditLogs(0, take);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual values passed
        _mockAuditService.Verify(x => x.GetUserAuditLogsAsync(userId, 0, take), Times.Once);
    }

    #endregion

    #region GetAuditLogsByAction Tests

    [Fact]
    public async Task GetAuditLogsByAction_WithValidParameters_ShouldReturnOkResult()
    {
        // Arrange
        SetupAuthenticatedUser();
        var action = "Created";
        var skip = 0;
        var take = 50;
        var expectedAuditLogs = new List<DataSetAuditLog>
        {
            new() { Id = 1, DataSetId = 1, UserId = "user1", Action = action, Timestamp = DateTime.UtcNow },
            new() { Id = 2, DataSetId = 2, UserId = "user2", Action = action, Timestamp = DateTime.UtcNow }
        };

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, skip, take))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action, skip, take);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetAuditLogsByActionAsync(action, skip, take), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsByAction_WithDefaultParameters_ShouldUseDefaultValues()
    {
        // Arrange
        SetupAuthenticatedUser();
        var action = "Updated";
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetAuditLogsByActionAsync(action, 0, 50), Times.Once);
    }

    [Theory]
    [InlineData("Created")]
    [InlineData("Updated")]
    [InlineData("Deleted")]
    [InlineData("Processed")]
    public async Task GetAuditLogsByAction_WithDifferentActionTypes_ShouldReturnOkResult(string action)
    {
        // Arrange
        SetupAuthenticatedUser();
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        _mockAuditService.Verify(x => x.GetAuditLogsByActionAsync(action, 0, 50), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsByAction_WhenServiceThrowsException_ShouldReturnErrorResponse()
    {
        // Arrange
        SetupAuthenticatedUser();
        var action = "Created";
        var exception = new InvalidOperationException("Database connection failed");

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, 0, 50))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetAuditLogsByAction(action);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(400);
        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetAuditLogsByAction_WithEmptyOrNullAction_ShouldPassThroughToService(string? action)
    {
        // Arrange
        SetupAuthenticatedUser();
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action!, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action!);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual action passed
        _mockAuditService.Verify(x => x.GetAuditLogsByActionAsync(action!, 0, 50), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsByAction_WithActionTooLong_ShouldPassThroughToService()
    {
        // Arrange
        SetupAuthenticatedUser();
        var action = new string('A', 51); // 51 characters, exceeds max of 50
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual action passed
        _mockAuditService.Verify(x => x.GetAuditLogsByActionAsync(action, 0, 50), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetAuditLogsByAction_WithNegativeSkip_ShouldPassThroughToService(int skip)
    {
        // Arrange
        SetupAuthenticatedUser();
        var action = "Created";
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, skip, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action, skip, 50);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual values passed
        _mockAuditService.Verify(x => x.GetAuditLogsByActionAsync(action, skip, 50), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(200)]
    public async Task GetAuditLogsByAction_WithInvalidTake_ShouldPassThroughToService(int take)
    {
        // Arrange
        SetupAuthenticatedUser();
        var action = "Created";
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, 0, take))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action, 0, take);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeTrue();

        // Verify that the service was called with the actual values passed
        _mockAuditService.Verify(x => x.GetAuditLogsByActionAsync(action, 0, take), Times.Once);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task GetUserAuditLogs_WithUnauthenticatedUser_ShouldReturnErrorResponse()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetUserAuditLogs();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        objectResult.StatusCode.Should().Be(401);
        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserAuditLogs_WithValidUser_ShouldExtractUserIdFromClaims()
    {
        // Arrange
        var expectedUserId = "auth0|test-user-123";
        var claims = new List<Claim>
        {
            new("sub", expectedUserId)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;

        var expectedAuditLogs = new List<DataSetAuditLog>();
        _mockAuditService
            .Setup(x => x.GetUserAuditLogsAsync(expectedUserId, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetUserAuditLogs();

        // Assert
        result.Should().NotBeNull();
        _mockAuditService.Verify(x => x.GetUserAuditLogsAsync(expectedUserId, 0, 50), Times.Once);
    }

    #endregion

    #region Method Attributes Tests

    [Fact]
    public void GetDataSetAuditLogs_ShouldHaveHttpGetAttribute()
    {
        // Act
        var method = typeof(AuditController).GetMethod("GetDataSetAuditLogs");
        var httpGetAttribute = method?.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .FirstOrDefault();

        // Assert
        httpGetAttribute.Should().NotBeNull();
        var httpGet = httpGetAttribute as HttpGetAttribute;
        httpGet!.Template.Should().Be("datasets/{dataSetId}");
    }

    [Fact]
    public void GetUserAuditLogs_ShouldHaveHttpGetAttribute()
    {
        // Act
        var method = typeof(AuditController).GetMethod("GetUserAuditLogs");
        var httpGetAttribute = method?.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .FirstOrDefault();

        // Assert
        httpGetAttribute.Should().NotBeNull();
        var httpGet = httpGetAttribute as HttpGetAttribute;
        httpGet!.Template.Should().Be("user");
    }

    [Fact]
    public void GetAuditLogsByAction_ShouldHaveHttpGetAttribute()
    {
        // Act
        var method = typeof(AuditController).GetMethod("GetAuditLogsByAction");
        var httpGetAttribute = method?.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .FirstOrDefault();

        // Assert
        httpGetAttribute.Should().NotBeNull();
        var httpGet = httpGetAttribute as HttpGetAttribute;
        httpGet!.Template.Should().Be("actions/{action}");
    }

    [Fact]
    public void GetDataSetAuditLogs_ShouldHaveProducesResponseTypeAttributes()
    {
        // Act
        var method = typeof(AuditController).GetMethod("GetDataSetAuditLogs");
        var producesResponseTypeAttributes = method?.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), true);

        // Assert
        producesResponseTypeAttributes.Should().NotBeNull();
        producesResponseTypeAttributes!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetUserAuditLogs_ShouldHaveProducesResponseTypeAttributes()
    {
        // Act
        var method = typeof(AuditController).GetMethod("GetUserAuditLogs");
        var producesResponseTypeAttributes = method?.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), true);

        // Assert
        producesResponseTypeAttributes.Should().NotBeNull();
        producesResponseTypeAttributes!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetAuditLogsByAction_ShouldHaveProducesResponseTypeAttributes()
    {
        // Act
        var method = typeof(AuditController).GetMethod("GetAuditLogsByAction");
        var producesResponseTypeAttributes = method?.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), true);

        // Assert
        producesResponseTypeAttributes.Should().NotBeNull();
        producesResponseTypeAttributes!.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task GetDataSetAuditLogs_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        SetupAuthenticatedUser();
        var dataSetId = 999;
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetDataSetAuditLogsAsync(dataSetId, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetDataSetAuditLogs(dataSetId);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().BeEmpty();
        apiResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserAuditLogs_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        var userId = "new-user-123";
        SetupAuthenticatedUser(userId);
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetUserAuditLogsAsync(userId, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetUserAuditLogs();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().BeEmpty();
        apiResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetAuditLogsByAction_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        SetupAuthenticatedUser();
        var action = "NonExistentAction";
        var expectedAuditLogs = new List<DataSetAuditLog>();

        _mockAuditService
            .Setup(x => x.GetAuditLogsByActionAsync(action, 0, 50))
            .ReturnsAsync(expectedAuditLogs);

        // Act
        var result = await _controller.GetAuditLogsByAction(action);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<IEnumerable<DataSetAuditLog>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().NotBeNull();
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DataSetAuditLog>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().BeEmpty();
        apiResponse.Success.Should().BeTrue();
    }

    #endregion
}