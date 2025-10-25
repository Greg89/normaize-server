using System;
using Xunit;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Tests.Entities;

public class DataSetTests
{
    private static FileMetadata CreateTestFileMetadata()
    {
        return FileMetadata.Create(
            "test.csv",
            "/uploads/test.csv",
            FileType.CSV,
            1024,
            "abc123");
    }

    private static DatasetStatistics CreateTestStatistics()
    {
        return DatasetStatistics.Create(100, 5);
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateDataSet()
    {
        // Arrange
        var name = "Test Dataset";
        var description = "Test description";
        var userId = "user123";
        var fileInfo = CreateTestFileMetadata();
        var statistics = CreateTestStatistics();

        // Act
        var dataSet = DataSet.Create(name, description, userId, fileInfo, statistics);

        // Assert
        Assert.NotEqual(Guid.Empty, dataSet.Id);
        Assert.Equal(name, dataSet.Name);
        Assert.Equal(description, dataSet.Description);
        Assert.Equal(userId, dataSet.UserId);
        Assert.Equal(fileInfo, dataSet.FileInfo);
        Assert.Equal(statistics, dataSet.Statistics);
        Assert.False(dataSet.IsDeleted);
        Assert.True(dataSet.UploadedAt <= DateTime.UtcNow);
        Assert.True(dataSet.LastModifiedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var fileInfo = CreateTestFileMetadata();
        var statistics = CreateTestStatistics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            DataSet.Create(invalidName, "description", "user123", fileInfo, statistics));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUserId_ShouldThrowArgumentException(string invalidUserId)
    {
        // Arrange
        var fileInfo = CreateTestFileMetadata();
        var statistics = CreateTestStatistics();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            DataSet.Create("Test Dataset", "description", invalidUserId, fileInfo, statistics));
    }

    [Fact]
    public void Create_WithNullFileInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var statistics = CreateTestStatistics();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            DataSet.Create("Test Dataset", "description", "user123", null!, statistics));
    }

    [Fact]
    public void Create_WithNullStatistics_ShouldUseEmptyStatistics()
    {
        // Arrange
        var fileInfo = CreateTestFileMetadata();

        // Act
        var dataSet = DataSet.Create("Test Dataset", "description", "user123", fileInfo, null);

        // Assert
        Assert.Equal(DatasetStatistics.Empty, dataSet.Statistics);
    }

    [Fact]
    public void Create_ShouldTrimNameAndDescription()
    {
        // Arrange
        var fileInfo = CreateTestFileMetadata();
        var statistics = CreateTestStatistics();

        // Act
        var dataSet = DataSet.Create("  Test Dataset  ", "  Test description  ", "  user123  ", fileInfo, statistics);

        // Assert
        Assert.Equal("Test Dataset", dataSet.Name);
        Assert.Equal("Test description", dataSet.Description);
        Assert.Equal("user123", dataSet.UserId);
    }

    [Fact]
    public void UpdateSchema_ShouldUpdateSchemaAndModificationInfo()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var originalModifiedAt = dataSet.LastModifiedAt;
        var schema = "{\"columns\": []}";
        var modifiedBy = "admin";

        // Wait a bit to ensure timestamp changes
        System.Threading.Thread.Sleep(1);

        // Act
        dataSet.UpdateSchema(schema, modifiedBy);

        // Assert
        Assert.Equal(schema, dataSet.Schema);
        Assert.Equal(modifiedBy, dataSet.LastModifiedBy);
        Assert.True(dataSet.LastModifiedAt > originalModifiedAt);
    }

    [Fact]
    public void UpdateStatistics_ShouldUpdateStatsAndModificationInfo()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var newStats = DatasetStatistics.Create(200, 10);
        var modifiedBy = "admin";

        // Act
        dataSet.UpdateStatistics(newStats, modifiedBy);

        // Assert
        Assert.Equal(newStats, dataSet.Statistics);
        Assert.Equal(modifiedBy, dataSet.LastModifiedBy);
    }

    [Fact]
    public void UpdateStatistics_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dataSet.UpdateStatistics(null!, "admin"));
    }

    [Fact]
    public void SetPreviewData_ShouldUpdatePreviewDataAndModificationInfo()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var previewData = "[{\"col1\": \"value1\"}]";

        // Act
        dataSet.SetPreviewData(previewData, "admin");

        // Assert
        Assert.Equal(previewData, dataSet.PreviewData);
        Assert.Equal("admin", dataSet.LastModifiedBy);
    }

    [Fact]
    public void SetProcessedData_ShouldUpdateDataAndMarkAsProcessed()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var processedData = "[{\"col1\": \"processed\"}]";

        // Act
        dataSet.SetProcessedData(processedData, "admin");

        // Assert
        Assert.Equal(processedData, dataSet.ProcessedData);
        Assert.True(dataSet.Statistics.IsProcessed);
        Assert.True(dataSet.IsProcessed);
        Assert.Equal("admin", dataSet.LastModifiedBy);
    }

    [Fact]
    public void RecordProcessingError_ShouldUpdateErrorAndModificationInfo()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var error = "Processing failed";

        // Act
        dataSet.RecordProcessingError(error, "system");

        // Assert
        Assert.Equal(error, dataSet.ProcessingErrors);
        Assert.True(dataSet.HasProcessingErrors);
        Assert.Equal("system", dataSet.LastModifiedBy);
    }

    [Fact]
    public void SetRetentionPolicy_WithValidDate_ShouldSetRetentionExpiryDate()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var expiryDate = DateTime.UtcNow.AddDays(30);

        // Act
        dataSet.SetRetentionPolicy(expiryDate, "admin");

        // Assert
        Assert.Equal(expiryDate, dataSet.RetentionExpiryDate);
        Assert.Equal("admin", dataSet.LastModifiedBy);
    }

    [Fact]
    public void SetRetentionPolicy_WithPastDate_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => dataSet.SetRetentionPolicy(pastDate, "admin"));
    }

    [Fact]
    public void Delete_WithValidDeletedBy_ShouldSoftDeleteDataSet()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var deletedBy = "admin";

        // Act
        dataSet.Delete(deletedBy);

        // Assert
        Assert.True(dataSet.IsDeleted);
        Assert.NotNull(dataSet.DeletedAt);
        Assert.Equal(deletedBy, dataSet.DeletedBy);
        Assert.Equal(deletedBy, dataSet.LastModifiedBy);
        Assert.True(dataSet.DeletedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Delete_WithInvalidDeletedBy_ShouldThrowArgumentException(string invalidDeletedBy)
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => dataSet.Delete(invalidDeletedBy));
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        dataSet.Delete("admin");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => dataSet.Delete("admin"));
    }

    [Fact]
    public void Restore_WithValidRestoredBy_ShouldRestoreDataSet()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        dataSet.Delete("admin");
        var restoredBy = "admin";

        // Act
        dataSet.Restore(restoredBy);

        // Assert
        Assert.False(dataSet.IsDeleted);
        Assert.Null(dataSet.DeletedAt);
        Assert.Null(dataSet.DeletedBy);
        Assert.Equal(restoredBy, dataSet.LastModifiedBy);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => dataSet.Restore("admin"));
    }

    [Fact]
    public void MarkAsProcessed_ShouldUpdateStatisticsAndModificationInfo()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var originalModifiedAt = dataSet.LastModifiedAt;

        // Wait a bit to ensure timestamp changes
        System.Threading.Thread.Sleep(1);

        // Act
        dataSet.MarkAsProcessed();

        // Assert
        Assert.True(dataSet.Statistics.IsProcessed);
        Assert.True(dataSet.IsProcessed);
        Assert.True(dataSet.LastModifiedAt > originalModifiedAt);
    }

    [Fact]
    public void BusinessRuleQueries_ShouldReturnCorrectValues()
    {
        // Arrange
        var fileInfo = CreateTestFileMetadata();
        var stats = DatasetStatistics.Create(100, 5);
        var dataSet = DataSet.Create("Test", "desc", "user123", fileInfo, stats);

        // Act & Assert
        Assert.False(dataSet.IsRetentionExpired); // No retention policy set
        Assert.False(dataSet.IsProcessed); // Not processed yet
        Assert.False(dataSet.IsLargeDataset); // Small dataset
        Assert.False(dataSet.RequiresSeparateTable); // Small dataset
        Assert.False(dataSet.HasProcessingErrors); // No errors
        Assert.True(dataSet.IsTextBasedFile); // CSV is text-based
        Assert.False(dataSet.IsStoredInCloud); // Local storage
    }

    [Fact]
    public void BusinessRuleQueries_WithRetentionExpired_ShouldReturnTrue()
    {
        // Arrange
        var dataSet = DataSet.Create("Test", "desc", "user123", CreateTestFileMetadata(), CreateTestStatistics());
        var expiredDate = DateTime.UtcNow.AddDays(-1);
        
        // Use reflection to set the retention date to a past date for testing
        var property = typeof(DataSet).GetProperty("RetentionExpiryDate");
        property?.SetValue(dataSet, expiredDate);

        // Act & Assert
        Assert.True(dataSet.IsRetentionExpired);
    }
}