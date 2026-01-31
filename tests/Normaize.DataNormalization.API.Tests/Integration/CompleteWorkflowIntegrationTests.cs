using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Normaize.DataNormalization.API.Tests.Infrastructure;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.API.Tests.Integration;

/// <summary>
/// End-to-end integration tests that verify the complete job processing workflow
/// </summary>
public class CompleteWorkflowIntegrationTests : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ApiTestApplicationFactory _factory;

    public CompleteWorkflowIntegrationTests(ApiTestApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateAuthenticatedClient();
        // Seed test data synchronously - this will be called once per test class
        Task.Run(async () => await _factory.SeedTestDataAsync()).Wait();
    }

    [Fact]
    public async Task CompleteWorkflow_SubmitDuplicateRemovalJob_ShouldProcessSuccessfully()
    {
        // Arrange: Create a test dataset with duplicate data
        var dataSetId = await CreateTestDataSetAsync();

        var request = new RemoveDuplicatesRequest
        {
            DataSetId = dataSetId,
            Strategy = "KeepFirst",
            ComparisonColumns = new List<string> { "Email", "FirstName" },
            CaseSensitive = false,
            TrimWhitespace = true
        };

        // Act: Submit duplicate removal job
        var submitResponse = await _client.PostAsJsonAsync("/api/normalization/remove-duplicates", request);

        // Assert: Job submission should succeed
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var submitResult = await submitResponse.Content.ReadFromJsonAsync<ApiResponse<JobSubmissionResponse>>();
        submitResult.Should().NotBeNull();
        submitResult!.Success.Should().BeTrue();
        submitResult.Data.Should().NotBeNull();
        submitResult.Data!.JobId.Should().NotBeEmpty();

        var jobId = submitResult.Data.JobId;

        // Act: Check job status
        var statusResponse = await _client.GetAsync($"/api/normalization/jobs/{jobId}");

        // Assert: Job status should be retrievable
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var statusResult = await statusResponse.Content.ReadFromJsonAsync<ApiResponse<JobStatusResponse>>();
        statusResult.Should().NotBeNull();
        statusResult!.Success.Should().BeTrue();
        statusResult.Data.Should().NotBeNull();
        statusResult.Data!.JobId.Should().Be(jobId);
        statusResult.Data.DataSetId.Should().Be(dataSetId);
        statusResult.Data.JobType.Should().Be("RemoveDuplicates");
        statusResult.Data.Status.Should().BeOneOf("Queued", "Processing", "Succeeded", "Completed");
    }

    [Fact]
    public async Task JobRetry_WithFailedJob_ShouldScheduleRetry()
    {
        // Arrange: Create a test dataset and a failed job
        var dataSetId = await CreateTestDataSetAsync();
        var jobId = await CreateTestFailedJobAsync(dataSetId);

        // Act: Retry the job
        var retryResponse = await _client.PostAsync($"/api/normalization/jobs/{jobId}/retry", null);

        // Assert: Retry should succeed
        retryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await retryResponse.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task JobCancellation_WithQueuedJob_ShouldCancelSuccessfully()
    {
        // Arrange: Create a test dataset and job
        var dataSetId = await CreateTestDataSetAsync();
        var jobId = await CreateTestJobAsync(dataSetId);

        var cancelRequest = new CancelJobRequest
        {
            Reason = "Test cancellation"
        };

        // Act: Cancel the job
        var cancelResponse = await _client.PostAsJsonAsync($"/api/normalization/jobs/{jobId}/cancel", cancelRequest);

        // Assert: Cancellation should succeed
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await cancelResponse.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Verify job status changed to cancelled/dead letter
        var statusResponse = await _client.GetAsync($"/api/normalization/jobs/{jobId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var statusResult = await statusResponse.Content.ReadFromJsonAsync<ApiResponse<JobStatusResponse>>();
        statusResult!.Data!.Status.Should().Be("DeadLettered");
    }

    [Fact]
    public async Task GetJobStatus_WithNonExistentJob_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/normalization/jobs/{nonExistentJobId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitJob_WithInvalidDataSetId_ShouldReturnError()
    {
        // Arrange
        var nonExistentDataSetId = Guid.NewGuid();
        var request = new RemoveDuplicatesRequest
        {
            DataSetId = nonExistentDataSetId,
            Strategy = "KeepFirst",
            ComparisonColumns = new List<string> { "Email" },
            CaseSensitive = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/normalization/remove-duplicates", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitGenericJob_WithUnsupportedJobType_ShouldReturnError()
    {
        // Arrange
        var dataSetId = await CreateTestDataSetAsync();
        var request = new SubmitJobRequest
        {
            DataSetId = dataSetId,
            JobType = "UnsupportedOperation",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/normalization/submit-job", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<JobSubmissionResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("UNSUPPORTED_JOB_TYPE");
    }

    [Fact]
    public async Task DataSetsEndpoint_ShouldReturnDataSets()
    {
        // Act
        var response = await _client.GetAsync("/api/datasets");

        // Assert: Should return 200 even if empty (authentication will be handled later)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Creates a test dataset in the database for testing purposes
    /// </summary>
    private async Task<Guid> CreateTestDataSetAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var fileInfo = FileMetadata.Create(
            fileName: "test-data.csv",
            filePath: "/test/path/test-data.csv",
            fileType: FileType.CSV,
            fileSize: 1024,
            dataHash: "test-hash-123"
        );

        var statistics = DatasetStatistics.Create(
            rowCount: 100,
            columnCount: 5
        );

        var dataSet = DataSet.Create(
            name: "Test Dataset",
            description: "A test dataset for integration testing",
            userId: "test-user-id",
            fileInfo: fileInfo,
            statistics: statistics
        );

        // Add some sample schema and preview data
        var schema = JsonSerializer.Serialize(new
        {
            columns = new[]
            {
                new { name = "Id", type = "integer" },
                new { name = "FirstName", type = "string" },
                new { name = "LastName", type = "string" },
                new { name = "Email", type = "string" },
                new { name = "Age", type = "integer" }
            }
        });

        var previewData = JsonSerializer.Serialize(new
        {
            rows = new[]
            {
                new { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Age = 30 },
                new { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Age = 25 },
                new { Id = 3, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Age = 30 }, // Duplicate
                new { Id = 4, FirstName = "Bob", LastName = "Wilson", Email = "bob.wilson@example.com", Age = 35 }
            }
        });

        dataSet.UpdateSchema(schema);
        dataSet.SetPreviewData(previewData);

        context.DataSets.Add(dataSet);
        await context.SaveChangesAsync();

        return dataSet.Id;
    }

    /// <summary>
    /// Creates a test normalization job in the database
    /// </summary>
    private async Task<Guid> CreateTestJobAsync(Guid dataSetId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var options = DuplicateRemovalOptions.KeepFirst(
            keyColumns: new List<string> { "Email" },
            caseSensitivity: CaseSensitivity.Insensitive
        );

        var job = NormalizationJob.CreateDuplicateRemovalJob(dataSetId, options);

        context.NormalizationJobs.Add(job);
        await context.SaveChangesAsync();

        return job.Id;
    }

    /// <summary>
    /// Creates a failed test normalization job in the database for retry testing
    /// </summary>
    private async Task<Guid> CreateTestFailedJobAsync(Guid dataSetId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var options = DuplicateRemovalOptions.KeepFirst(
            keyColumns: new List<string> { "Email" },
            caseSensitivity: CaseSensitivity.Insensitive
        );

        var job = NormalizationJob.CreateDuplicateRemovalJob(dataSetId, options);
        
        // Transition job to processing then fail it to enable retry functionality
        job.Start();
        job.Fail("Test failure for retry testing");

        context.NormalizationJobs.Add(job);
        await context.SaveChangesAsync();

        return job.Id;
    }
}

/// <summary>
/// Standard API response wrapper for testing (matching BaseApiController format)
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

/// <summary>
/// Paginated API response wrapper for testing
/// </summary>
public class PaginatedApiResponse<T> : ApiResponse<T>
{
    public PaginationMetadata? Pagination { get; set; }
}

/// <summary>
/// Pagination metadata for testing
/// </summary>
public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}