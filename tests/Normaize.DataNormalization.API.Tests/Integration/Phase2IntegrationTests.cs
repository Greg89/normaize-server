using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.API.Tests.Infrastructure;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.API.Tests.Integration;

/// <summary>
/// Phase 2 integration tests focused on data consistency & quality.
/// Currently covers jobs list pagination correctness.
/// </summary>
public class Phase2IntegrationTests : IClassFixture<ApiTestApplicationFactory>
{
    private readonly ApiTestApplicationFactory _factory;
    private readonly HttpClient _client;

    private const string TestUserId = "test-user-id";

    public Phase2IntegrationTests(ApiTestApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateAuthenticatedClient(userId: TestUserId);
    }

    [Fact]
    public async Task GetUserJobs_ShouldReturnCorrectTotalItemsAndPageItems()
    {
        await ResetDatabaseAsync();

        // Arrange
        var dataSetId = await CreateTestDataSetAsync(TestUserId);
        await CreateQueuedJobsAsync(dataSetId, count: 25);

        // Act
        var response = await _client.GetAsync("/api/normalization/jobs?page=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedApiResponse<JobListResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Pagination.Should().NotBeNull();

        result.Data!.TotalJobs.Should().Be(25);
        result.Data.Jobs.Should().HaveCount(10);

        result.Pagination!.Page.Should().Be(2);
        result.Pagination.PageSize.Should().Be(10);
        result.Pagination.TotalItems.Should().Be(25);
    }

    [Fact]
    public async Task GetUserJobs_StatusCompletedFilter_ShouldMatchSucceededJobs()
    {
        await ResetDatabaseAsync();

        // Arrange
        var dataSetId = await CreateTestDataSetAsync(TestUserId);
        await CreateSucceededJobsAsync(dataSetId, count: 3);
        await CreateQueuedJobsAsync(dataSetId, count: 2);

        // Act
        var response = await _client.GetAsync("/api/normalization/jobs?status=Completed&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedApiResponse<JobListResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        result.Data!.TotalJobs.Should().Be(3);
        result.Data.Jobs.Should().HaveCount(3);
        result.Data.Jobs.Should().OnlyContain(j => j.Status == "Succeeded");
    }

    [Fact]
    public async Task GetDataSetJobs_ShouldOnlyReturnJobsForThatDataSet()
    {
        await ResetDatabaseAsync();

        // Arrange
        var dataSetId1 = await CreateTestDataSetAsync(TestUserId);
        var dataSetId2 = await CreateTestDataSetAsync(TestUserId);

        await CreateQueuedJobsAsync(dataSetId1, count: 7);
        await CreateQueuedJobsAsync(dataSetId2, count: 4);

        // Act
        var response = await _client.GetAsync($"/api/normalization/datasets/{dataSetId1}/jobs?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedApiResponse<JobListResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        result.Data!.TotalJobs.Should().Be(7);
        result.Data.Jobs.Should().HaveCount(7);
        result.Data.Jobs.Should().OnlyContain(j => j.DataSetId == dataSetId1);
    }

    private async Task<Guid> CreateTestDataSetAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var fileInfo = FileMetadata.Create(
            fileName: "test.csv",
            filePath: "/test/test.csv",
            fileType: FileType.CSV,
            fileSize: 1024,
            dataHash: Guid.NewGuid().ToString("N"));

        var stats = DatasetStatistics.Create(100, 5);

        var dataSet = DataSet.Create(
            name: $"Test Dataset {Guid.NewGuid():N}",
            description: "Test dataset for integration tests",
            userId: userId,
            fileInfo: fileInfo,
            statistics: stats);

        // Add minimal schema/preview to keep downstream endpoints happy if used.
        dataSet.UpdateSchema(JsonSerializer.Serialize(new { columns = new[] { new { name = "Email", type = "string" } } }));
        dataSet.SetPreviewData(JsonSerializer.Serialize(new { rows = Array.Empty<object>() }));

        context.DataSets.Add(dataSet);
        await context.SaveChangesAsync();

        return dataSet.Id;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private async Task CreateQueuedJobsAsync(Guid dataSetId, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var options = DuplicateRemovalOptions.KeepFirst(
            keyColumns: new List<string> { "Email" },
            caseSensitivity: CaseSensitivity.Insensitive);

        var jobs = Enumerable.Range(0, count)
            .Select(_ => NormalizationJob.CreateDuplicateRemovalJob(dataSetId, options))
            .ToList();

        context.NormalizationJobs.AddRange(jobs);
        await context.SaveChangesAsync();
    }

    private async Task CreateSucceededJobsAsync(Guid dataSetId, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var options = DuplicateRemovalOptions.KeepFirst(
            keyColumns: new List<string> { "Email" },
            caseSensitivity: CaseSensitivity.Insensitive);

        var jobs = new List<NormalizationJob>();
        for (var i = 0; i < count; i++)
        {
            var job = NormalizationJob.CreateDuplicateRemovalJob(dataSetId, options);
            job.Start();
            job.Complete("{}");
            jobs.Add(job);
        }

        context.NormalizationJobs.AddRange(jobs);
        await context.SaveChangesAsync();
    }
}
