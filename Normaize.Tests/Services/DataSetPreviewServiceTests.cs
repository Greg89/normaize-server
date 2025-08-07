using Moq;
using FluentAssertions;
using Xunit;
using Normaize.Core.Services;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using Normaize.Core.Constants;
using System.Security.Claims;
using System.Text.Json;

namespace Normaize.Tests.Services;

public class DataSetPreviewServiceTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IDataProcessingInfrastructure> _mockInfrastructure;
    private readonly Mock<IStructuredLoggingService> _mockStructuredLogging;
    private readonly Mock<IChaosEngineeringService> _mockChaosEngineering;
    private readonly Mock<IOperationContext> _mockOperationContext;
    private readonly DataSetPreviewService _service;

    public DataSetPreviewServiceTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockInfrastructure = new Mock<IDataProcessingInfrastructure>();
        _mockStructuredLogging = new Mock<IStructuredLoggingService>();
        _mockChaosEngineering = new Mock<IChaosEngineeringService>();
        _mockOperationContext = new Mock<IOperationContext>();

        _mockInfrastructure.Setup(x => x.StructuredLogging).Returns(_mockStructuredLogging.Object);
        _mockInfrastructure.Setup(x => x.ChaosEngineering).Returns(_mockChaosEngineering.Object);

        _mockOperationContext.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        _mockOperationContext.Setup(x => x.OperationName).Returns("TestOperation");

        _mockStructuredLogging
            .Setup(x => x.CreateContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>()))
            .Returns(_mockOperationContext.Object);

        _service = new DataSetPreviewService(
            _mockRepository.Object,
            _mockInfrastructure.Object);
    }

    #region GetDataSetPreviewAsync Tests

    [Fact]
    public async Task GetDataSetPreviewAsync_WithValidInputs_ShouldReturnPreview()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            PreviewData = "{\"columns\":[\"name\",\"age\"],\"rows\":[{\"name\":\"John\",\"age\":30},{\"name\":\"Jane\",\"age\":25}],\"totalRows\":2,\"previewRowCount\":5,\"maxPreviewRows\":1000}"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Rows.Should().NotBeNull();
        result.Rows.Should().HaveCount(2);
        result.TotalRows.Should().Be(2);
        result.PreviewRowCount.Should().Be(rows);

        _mockRepository.Verify(x => x.GetByIdAsync(dataSetId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithUnprocessedDataset_ShouldReturnNull()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = false
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().BeNull();

        _mockRepository.Verify(x => x.GetByIdAsync(dataSetId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithNullPreviewData_ShouldReturnNull()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            PreviewData = null
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            PreviewData = "invalid json"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithLargeRowCount_ShouldLimitToMaxRows()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 1000; // Larger than max
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            PreviewData = "{\"columns\":[\"name\",\"age\"],\"rows\":[{\"name\":\"John\",\"age\":30},{\"name\":\"Jane\",\"age\":25}],\"totalRows\":2,\"previewRowCount\":5,\"maxPreviewRows\":1000}"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().NotBeNull();
        result!.PreviewRowCount.Should().Be(AppConstants.DataSetPreview.MAX_PREVIEW_ROWS);
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithInvalidDataSetId_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "test-user";
        var rows = 5;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetDataSetPreviewAsync(dataSetId, rows, userId!));
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 1;
        string? userId = null;
        var rows = 5;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetDataSetPreviewAsync(dataSetId, rows, userId));
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithInvalidRows_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 0;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetDataSetPreviewAsync(dataSetId, rows, userId));
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithDatasetNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync((DataSet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetDataSetPreviewAsync(dataSetId, rows, userId));
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var differentUserId = "different-user";
        var rows = 5;
        var dataSet = new DataSet { Id = dataSetId, UserId = differentUserId };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetDataSetPreviewAsync(dataSetId, rows, userId));
    }

    [Fact]
    public async Task GetDataSetPreviewAsync_WithMoreRequestedRowsThanAvailable_ShouldReturnAllAvailable()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 10; // Request more than available
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            PreviewData = "{\"columns\":[\"name\",\"age\"],\"rows\":[{\"name\":\"John\",\"age\":30},{\"name\":\"Jane\",\"age\":25}],\"totalRows\":2,\"previewRowCount\":5,\"maxPreviewRows\":1000}"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Rows.Should().HaveCount(2); // Only 2 available
        result.TotalRows.Should().Be(2);
        result.PreviewRowCount.Should().Be(rows);
    }

    #endregion

    #region GetDataSetSchemaAsync Tests

    [Fact]
    public async Task GetDataSetSchemaAsync_WithValidInputs_ShouldReturnSchema()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            Schema = "[\"name\",\"age\",\"email\"]"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetSchemaAsync(dataSetId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<string>>();
        var schemaList = result as List<string>;
        schemaList.Should().HaveCount(3);
        schemaList.Should().Contain("name");
        schemaList.Should().Contain("age");
        schemaList.Should().Contain("email");

        _mockRepository.Verify(x => x.GetByIdAsync(dataSetId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithUnprocessedDataset_ShouldReturnNull()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = false
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetSchemaAsync(dataSetId, userId);

        // Assert
        result.Should().BeNull();

        _mockRepository.Verify(x => x.GetByIdAsync(dataSetId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithNullSchema_ShouldReturnNull()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            Schema = null
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetSchemaAsync(dataSetId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            Schema = "invalid json"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetSchemaAsync(dataSetId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithInvalidDataSetId_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "test-user";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetDataSetSchemaAsync(dataSetId, userId));
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 1;
        string? userId = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetDataSetSchemaAsync(dataSetId, userId!));
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithDatasetNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync((DataSet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetDataSetSchemaAsync(dataSetId, userId));
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var differentUserId = "different-user";
        var dataSet = new DataSet { Id = dataSetId, UserId = differentUserId };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetDataSetSchemaAsync(dataSetId, userId));
    }

    #endregion

    #region JSON Deserialization Tests

    [Fact]
    public async Task GetDataSetPreviewAsync_WithComplexJsonData_ShouldDeserializeCorrectly()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;
        var complexJson = @"{
            ""columns"":[""name"",""age"",""address""],
            ""rows"":[
                {""name"":""John"",""age"":30,""address"":{""city"":""New York"",""zip"":""10001""}},
                {""name"":""Jane"",""age"":25,""address"":{""city"":""Los Angeles"",""zip"":""90210""}}
            ],
            ""totalRows"":2,
            ""previewRowCount"":5,
            ""maxPreviewRows"":1000
        }";
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            PreviewData = complexJson
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Rows.Should().HaveCount(2);

        var firstRow = result.Rows.First();
        firstRow.Should().NotBeNull();
        firstRow["name"].ToString().Should().Be("John");
        ((System.Text.Json.JsonElement)firstRow["age"]).GetInt32().Should().Be(30);
        ((System.Text.Json.JsonElement)firstRow["address"]).GetProperty("city").GetString().Should().Be("New York");
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithComplexSchema_ShouldDeserializeCorrectly()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            Schema = "[\"id\",\"name\",\"email\",\"created_at\",\"status\"]"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetSchemaAsync(dataSetId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<string>>();
        var schemaList = result as List<string>;
        schemaList.Should().HaveCount(5);
        schemaList.Should().Contain("id");
        schemaList.Should().Contain("name");
        schemaList.Should().Contain("email");
        schemaList.Should().Contain("created_at");
        schemaList.Should().Contain("status");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetDataSetPreviewAsync_WhenRepositoryThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;
        var exception = new Exception("Database error");

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.GetDataSetPreviewAsync(dataSetId, rows, userId));

        _mockStructuredLogging.Verify(x => x.LogException(exception, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WhenRepositoryThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var exception = new Exception("Database error");

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.GetDataSetSchemaAsync(dataSetId, userId));

        _mockStructuredLogging.Verify(x => x.LogException(exception, It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetDataSetPreviewAsync_WithEmptyJsonArray_ShouldReturnEmptyPreview()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var rows = 5;
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            PreviewData = "{\"columns\":[],\"rows\":[],\"totalRows\":0,\"previewRowCount\":5,\"maxPreviewRows\":1000}"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetPreviewAsync(dataSetId, rows, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Rows.Should().BeEmpty();
        result.TotalRows.Should().Be(0);
    }

    [Fact]
    public async Task GetDataSetSchemaAsync_WithEmptyJsonArray_ShouldReturnEmptySchema()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet
        {
            Id = dataSetId,
            UserId = userId,
            IsProcessed = true,
            Schema = "[]"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetSchemaAsync(dataSetId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<string>>();
        var schemaList = result as List<string>;
        schemaList.Should().BeEmpty();
    }

    #endregion
}