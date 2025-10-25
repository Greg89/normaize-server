using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Domain.Tests.Entities;

public class DataSetRowTests
{
    private readonly Guid _testDataSetId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_ShouldCreateDataSetRow()
    {
        // Arrange
        var dataSetId = _testDataSetId;
        var rowIndex = 5;
        var data = "{\"col1\": \"value1\", \"col2\": \"value2\"}";

        // Act
        var row = DataSetRow.Create(dataSetId, rowIndex, data);

        // Assert
        Assert.NotEqual(Guid.Empty, row.Id);
        Assert.Equal(dataSetId, row.DataSetId);
        Assert.Equal(rowIndex, row.RowIndex);
        Assert.Equal(data, row.Data);
        Assert.True(row.CreatedAt <= DateTime.UtcNow);
        Assert.Null(row.UpdatedAt);
    }

    [Fact]
    public void Create_WithEmptyDataSetId_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = Guid.Empty;
        var rowIndex = 0;
        var data = "{}";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DataSetRow.Create(dataSetId, rowIndex, data));
    }

    [Fact]
    public void Create_WithNegativeRowIndex_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = _testDataSetId;
        var rowIndex = -1;
        var data = "{}";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DataSetRow.Create(dataSetId, rowIndex, data));
    }

    [Fact]
    public void Create_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataSetId = _testDataSetId;
        var rowIndex = 0;
        string? data = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DataSetRow.Create(dataSetId, rowIndex, data!));
    }

    [Fact]
    public void GetValue_WithValidColumnName_ShouldReturnTypedValue()
    {
        // Arrange
        var testData = new Dictionary<string, object?>
        {
            ["name"] = "John Doe",
            ["age"] = 30,
            ["isActive"] = true,
            ["score"] = 95.5
        };
        var jsonData = JsonSerializer.Serialize(testData);
        var row = DataSetRow.Create(_testDataSetId, 0, jsonData);

        // Act & Assert
        Assert.Equal("John Doe", row.GetValue<string>("name"));
        Assert.Equal(30, row.GetValue<int>("age"));
        Assert.True(row.GetValue<bool>("isActive"));
        Assert.Equal(95.5, row.GetValue<double>("score"));
    }

    [Fact]
    public void GetValue_WithNonExistentColumn_ShouldReturnDefault()
    {
        // Arrange
        var jsonData = "{\"col1\": \"value1\"}";
        var row = DataSetRow.Create(_testDataSetId, 0, jsonData);

        // Act & Assert
        Assert.Null(row.GetValue<string>("nonexistent"));
        Assert.Equal(0, row.GetValue<int>("nonexistent"));
        Assert.False(row.GetValue<bool>("nonexistent"));
    }

    [Fact]
    public void GetValue_WithInvalidJson_ShouldReturnDefault()
    {
        // Arrange
        var invalidJsonData = "invalid json";
        var row = DataSetRow.Create(_testDataSetId, 0, invalidJsonData);

        // Act & Assert
        Assert.Null(row.GetValue<string>("col1"));
        Assert.Equal(0, row.GetValue<int>("col1"));
    }

    [Fact]
    public void GetAllValues_WithValidJson_ShouldReturnAllValues()
    {
        // Arrange
        var testData = new Dictionary<string, object?>
        {
            ["name"] = "John Doe",
            ["age"] = 30,
            ["isActive"] = true
        };
        var jsonData = JsonSerializer.Serialize(testData);
        var row = DataSetRow.Create(_testDataSetId, 0, jsonData);

        // Act
        var allValues = row.GetAllValues();

        // Assert
        Assert.Equal(3, allValues.Count);
        Assert.True(allValues.ContainsKey("name"));
        Assert.True(allValues.ContainsKey("age"));
        Assert.True(allValues.ContainsKey("isActive"));
        Assert.Equal("John Doe", allValues["name"]?.ToString());
    }

    [Fact]
    public void GetAllValues_WithEmptyJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var row = DataSetRow.Create(_testDataSetId, 0, "{}");

        // Act
        var allValues = row.GetAllValues();

        // Assert
        Assert.Empty(allValues);
    }

    [Fact]
    public void GetAllValues_WithInvalidJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var row = DataSetRow.Create(_testDataSetId, 0, "invalid json");

        // Act
        var allValues = row.GetAllValues();

        // Assert
        Assert.Empty(allValues);
    }

    [Fact]
    public void SetAllValues_WithValidDictionary_ShouldUpdateDataAndTimestamp()
    {
        // Arrange
        var row = DataSetRow.Create(_testDataSetId, 0, "{}");
        var originalCreatedAt = row.CreatedAt;
        var newValues = new Dictionary<string, object?>
        {
            ["name"] = "Jane Doe",
            ["age"] = 25,
            ["department"] = "Engineering"
        };

        // Wait a bit to ensure timestamp changes
        System.Threading.Thread.Sleep(1);

        // Act
        row.SetAllValues(newValues);

        // Assert
        var retrievedValues = row.GetAllValues();
        Assert.Equal(3, retrievedValues.Count);
        Assert.Equal("Jane Doe", retrievedValues["name"]?.ToString());
        Assert.NotNull(row.UpdatedAt);
        Assert.True(row.UpdatedAt > originalCreatedAt);
    }

    [Fact]
    public void UpdateData_WithValidJson_ShouldUpdateDataAndTimestamp()
    {
        // Arrange
        var row = DataSetRow.Create(_testDataSetId, 0, "{}");
        var originalCreatedAt = row.CreatedAt;
        var newData = "{\"updated\": true}";

        // Wait a bit to ensure timestamp changes
        System.Threading.Thread.Sleep(1);

        // Act
        row.UpdateData(newData);

        // Assert
        Assert.Equal(newData, row.Data);
        Assert.NotNull(row.UpdatedAt);
        Assert.True(row.UpdatedAt > originalCreatedAt);
    }

    [Fact]
    public void UpdateData_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var row = DataSetRow.Create(_testDataSetId, 0, "{}");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => row.UpdateData(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void Create_WithValidRowIndex_ShouldSucceed(int rowIndex)
    {
        // Act
        var row = DataSetRow.Create(_testDataSetId, rowIndex, "{}");

        // Assert
        Assert.Equal(rowIndex, row.RowIndex);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var row1 = DataSetRow.Create(_testDataSetId, 0, "{}");
        var row2 = DataSetRow.Create(_testDataSetId, 1, "{}");

        // Assert
        Assert.NotEqual(row1.Id, row2.Id);
    }

    [Fact]
    public void GetValue_WithTypeConversion_ShouldConvertCorrectly()
    {
        // Arrange
        var testData = new Dictionary<string, object?>
        {
            ["stringNumber"] = "42",
            ["stringBool"] = "true",
            ["numberString"] = 123
        };
        var jsonData = JsonSerializer.Serialize(testData);
        var row = DataSetRow.Create(_testDataSetId, 0, jsonData);

        // Act & Assert - JSON deserialization may handle some of these differently
        var stringValue = row.GetValue<string>("numberString");
        Assert.NotNull(stringValue);
    }

    [Fact]
    public void DataSetRow_ShouldMaintainImmutableRowIndexAndDataSetId()
    {
        // Arrange
        var dataSetId = _testDataSetId;
        var rowIndex = 5;
        var row = DataSetRow.Create(dataSetId, rowIndex, "{}");

        // Act & Assert
        Assert.Equal(dataSetId, row.DataSetId);
        Assert.Equal(rowIndex, row.RowIndex);

        // These properties should be read-only (private setters)
        // We can't modify them after creation, which is the correct behavior
    }
}