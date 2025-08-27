using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Services.DataNormalization;
using Normaize.Tests.Repositories;
using Xunit;

namespace Normaize.Tests.Services;

[Trait("Category", TestSetup.Categories.Unit)]
public class DuplicateRowRemovalProcessorTests
{
    private readonly Mock<ILogger<DuplicateRowRemovalProcessor>> _mockLogger;
    private readonly Mock<IDataSetRowRepository> _mockRepository;
    private readonly DuplicateRowRemovalProcessor _processor;
    public DuplicateRowRemovalProcessorTests()
    {
        _mockLogger = new Mock<ILogger<DuplicateRowRemovalProcessor>>();
        _mockRepository = new Mock<IDataSetRowRepository>();
        _processor = new DuplicateRowRemovalProcessor(_mockLogger.Object, _mockRepository.Object);
    }

    [Fact]
    public async Task ProcessAsync_WithValidRequest_ShouldRemoveDuplicates()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 5, useSeparateTable: true);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name", "Age"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var progressCallback = new Mock<IProgress<int>>();
        var rows = TestDataBuilder.CreateDataSetRowsWithDuplicates(dataSet.Id, 5);

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(rows);

        // Act
        var result = await _processor.ProcessAsync(dataSet, request, progressCallback.Object);

        // Assert
        result.Should().NotBeNull();
        result.RowsProcessed.Should().Be(5);
        result.DuplicateRowsRemoved.Should().BeGreaterThan(0);
        result.RowsRemaining.Should().BeLessThan(5);
        result.ProcessingTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.MemoryUsageMB.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessAsync_WithCaseSensitiveRequest_ShouldRespectCase()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 4);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = true
        };
        var progressCallback = new Mock<IProgress<int>>();
        var rows = TestDataBuilder.CreateDataSetRowsWithCaseVariations(dataSet.Id, 4);

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(rows);

        // Act
        var result = await _processor.ProcessAsync(dataSet, request, progressCallback.Object);

        // Assert
        result.Should().NotBeNull();
        result.DuplicateRowsRemoved.Should().Be(0); // Case sensitive, so "John" and "john" are different
    }

    [Fact]
    public async Task ProcessAsync_WithKeepLastOccurrence_ShouldKeepLastDuplicate()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 3, useSeparateTable: true);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = false,
            CaseSensitive = false
        };
        var progressCallback = new Mock<IProgress<int>>();
        var rows = TestDataBuilder.CreateDataSetRowsWithDuplicates(dataSet.Id, 3);

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(rows);

        // Act
        var result = await _processor.ProcessAsync(dataSet, request, progressCallback.Object);

        // Assert
        result.Should().NotBeNull();
        result.DuplicateRowsRemoved.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 1000);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var progressCallback = new Mock<IProgress<int>>();
        var cancellationTokenSource = new CancellationTokenSource();
        var rows = TestDataBuilder.CreateDataSetRows(dataSet.Id, 1000);

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(rows);

        // Act & Assert
        cancellationTokenSource.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _processor.ProcessAsync(dataSet, request, progressCallback.Object, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ValidateRequestAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 10);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name", "Age"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var result = await _processor.ValidateRequestAsync(dataSet, request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRequestAsync_WithEmptyColumnNames_ShouldReturnFailure()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 10);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = [],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var result = await _processor.ValidateRequestAsync(dataSet, request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateRequestAsync_WithTooManyColumns_ShouldReturnFailure()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 10);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = Enumerable.Range(1, 15).Select(i => $"Column{i}").ToArray(),
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var result = await _processor.ValidateRequestAsync(dataSet, request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Maximum");
    }

    [Fact]
    public async Task ValidateRequestAsync_WithUnprocessedDataset_ShouldReturnFailure()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: false, rowCount: 10);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var result = await _processor.ValidateRequestAsync(dataSet, request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("must be processed");
    }

    [Fact]
    public async Task ValidateRequestAsync_WithEmptyDataset_ShouldReturnFailure()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 0);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var result = await _processor.ValidateRequestAsync(dataSet, request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no rows to process");
    }

    [Fact]
    public async Task EstimateProcessingTimeAsync_ShouldReturnReasonableEstimate()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 1000);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name", "Age", "City"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var result = await _processor.EstimateProcessingTimeAsync(dataSet, request);

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(100000); // Should be less than 100 seconds for 1000 rows
    }

    [Fact]
    public async Task EstimateMemoryUsageAsync_ShouldReturnReasonableEstimate()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 1000, columnCount: 5);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name", "Age"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var result = await _processor.EstimateMemoryUsageAsync(dataSet, request);

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(1000); // Should be less than 1GB for 1000 rows
    }

    [Fact]
    public async Task ProcessAsync_WithMalformedRowData_ShouldSkipAndContinue()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 3, useSeparateTable: true);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var progressCallback = new Mock<IProgress<int>>();
        var rows = TestDataBuilder.CreateDataSetRowsWithMalformedData(dataSet.Id, 3);

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(rows);

        // Act
        var result = await _processor.ProcessAsync(dataSet, request, progressCallback.Object);

        // Assert
        result.Should().NotBeNull();
        result.RowsProcessed.Should().Be(3);
        // Should still process successfully even with malformed data
    }

    [Fact]
    public async Task ProcessAsync_WithLargeDataset_ShouldHandleProgressUpdates()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 1000, useSeparateTable: true);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var progressCallback = new Mock<IProgress<int>>();
        var rows = TestDataBuilder.CreateDataSetRows(dataSet.Id, 1000);

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(rows);

        // Act
        var result = await _processor.ProcessAsync(dataSet, request, progressCallback.Object);

        // Assert
        result.Should().NotBeNull();
        // Progress is reported more frequently than every 100 rows
        progressCallback.Verify(p => p.Report(It.IsAny<int>()), Times.AtLeast(10));
    }

    [Fact]
    public async Task ProcessAsync_WithSeparateTable_ShouldUseRepositoryMethods()
    {
        // Arrange
        var dataSet = TestDataBuilder.CreateDataSet(processed: true, rowCount: 5, useSeparateTable: true);
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var progressCallback = new Mock<IProgress<int>>();
        var rows = TestDataBuilder.CreateDataSetRows(dataSet.Id, 5);

        _mockRepository.Setup(r => r.GetByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(rows);
        _mockRepository.Setup(r => r.DeleteByDataSetIdAsync(dataSet.Id))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()))
            .ReturnsAsync(rows);

        // Act
        var result = await _processor.ProcessAsync(dataSet, request, progressCallback.Object);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.DeleteByDataSetIdAsync(dataSet.Id), Times.Once);
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()), Times.Once);
    }
}
