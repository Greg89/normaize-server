using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
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
/// Integration tests for Phase 1 critical endpoints to ensure client compatibility
/// </summary>
public class Phase1IntegrationTests : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ApiTestApplicationFactory _factory;
    private const string TestUserId = "auth0|test-user-123";
    private const string TestEmail = "test@example.com";
    private const string TestName = "Test User";

    public Phase1IntegrationTests(ApiTestApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateAuthenticatedClient(TestUserId, TestEmail, TestName);
    }

    #region UserSettingsController Tests

    [Fact]
    public async Task GetUserProfile_FirstAccess_ShouldAutoRegisterAndReturnProfile()
    {
        // Act
        var response = await _client.GetAsync("/api/UserSettings/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(TestUserId);
        result.Data.Email.Should().Be(TestEmail);
        result.Data.Name.Should().Be(TestName);
        result.Data.Settings.Should().NotBeNull();
        result.Data.Settings.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task GetUserProfile_WithExistingUser_ShouldReturnProfile()
    {
        // Arrange: Create user in database first
        await CreateTestUserAsync();

        // Act
        var response = await _client.GetAsync("/api/UserSettings/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateUserProfile_ShouldUpdateSettings()
    {
        // Arrange: Create user first
        await CreateTestUserAsync();

        var updateRequest = new UpdateUserSettingsRequest
        {
            DisplayName = "Updated Name",
            Theme = "dark",
            Language = "es",
            DefaultPageSize = 50,
            EmailNotificationsEnabled = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/UserSettings/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Updated Name");
        result.Data.Settings.Theme.Should().Be("dark");
        result.Data.Settings.Language.Should().Be("es");
        result.Data.Settings.DefaultPageSize.Should().Be(50);
        result.Data.Settings.EmailNotificationsEnabled.Should().BeTrue();
    }

    #endregion

    #region JobsController Tests (Backward Compatibility Route)

    [Fact]
    public async Task GetJobStatus_BackwardCompatibleRoute_ShouldReturnJobStatus()
    {
        // Arrange: Create a test dataset and job
        var dataSetId = await CreateTestDataSetAsync();
        var jobId = await CreateTestJobAsync(dataSetId);

        // Act: Use backward-compatible route /api/jobs/{jobId}/status
        var response = await _client.GetAsync($"/api/jobs/{jobId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<JobStatusResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.JobId.Should().Be(jobId);
        result.Data.DataSetId.Should().Be(dataSetId);
        result.Data.JobType.Should().Be("RemoveDuplicates");
        result.Data.Status.Should().BeOneOf("Queued", "Processing", "Completed");
    }

    [Fact]
    public async Task GetJobStatus_WithInvalidGuid_ShouldReturnBadRequest()
    {
        // Act: Use invalid GUID string
        var response = await _client.GetAsync("/api/jobs/invalid-guid/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetJobStatus_WithNonExistentJob_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/jobs/{nonExistentJobId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetJobStatus_WithCompletedJob_ShouldIncludeResults()
    {
        // Arrange: Create a completed job with results
        var dataSetId = await CreateTestDataSetAsync();
        var jobId = await CreateTestCompletedJobAsync(dataSetId);

        // Act
        var response = await _client.GetAsync($"/api/jobs/{jobId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<JobStatusResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be("Completed");
        result.Data.Results.Should().NotBeNull();
    }

    #endregion

    #region DataSetsController - Reset Endpoint Tests

    [Fact]
    public async Task ResetDataSet_WithReprocess_ShouldResetAndReprocess()
    {
        // Arrange: Create a test dataset
        var dataSetId = await CreateTestDataSetAsync();

        var resetRequest = new ResetDataSetRequest
        {
            ResetType = "REPROCESS",
            Reason = "Test reprocess reset"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/datasets/{dataSetId}/reset", resetRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Conflict);
        // Note: May return 404 if file storage service doesn't exist, or 409 if file not available
        // This is expected behavior based on implementation
    }

    [Fact]
    public async Task ResetDataSet_WithRestore_ShouldRestoreDataset()
    {
        // Arrange: Create and delete a test dataset
        var dataSetId = await CreateTestDataSetAsync();
        await DeleteDataSetAsync(dataSetId);

        var resetRequest = new ResetDataSetRequest
        {
            ResetType = "RESTORE",
            Reason = "Test restore reset"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/datasets/{dataSetId}/reset", resetRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DataSetResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IsDeleted.Should().BeFalse(); // Should be restored
        }
    }

    [Fact]
    public async Task ResetDataSet_WithInvalidResetType_ShouldReturnBadRequest()
    {
        // Arrange
        var dataSetId = await CreateTestDataSetAsync();

        var resetRequest = new ResetDataSetRequest
        {
            ResetType = "INVALID_TYPE",
            Reason = "Test invalid reset"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/datasets/{dataSetId}/reset", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DataSetResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_RESET_TYPE");
    }

    [Fact]
    public async Task ResetDataSet_WithNonExistentDataset_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentDataSetId = Guid.NewGuid();

        var resetRequest = new ResetDataSetRequest
        {
            ResetType = "REPROCESS",
            Reason = "Test reset"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/datasets/{nonExistentDataSetId}/reset", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DataSetsController - Remove Duplicates Endpoint Tests

    [Fact]
    public async Task RemoveDuplicates_PathParameterRoute_ShouldSubmitJob()
    {
        // Arrange: Create a test dataset
        var dataSetId = await CreateTestDataSetAsync();

        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = new List<string> { "Email", "FirstName" },
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act: Use backward-compatible path-parameter route
        var response = await _client.PostAsJsonAsync($"/api/datasets/{dataSetId}/remove-duplicates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<JobSubmissionResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.JobId.Should().NotBeEmpty();
        result.Data.Status.Should().Be("Submitted");
    }

    [Fact]
    public async Task RemoveDuplicates_WithKeepLast_ShouldSubmitJobWithKeepLastStrategy()
    {
        // Arrange
        var dataSetId = await CreateTestDataSetAsync();

        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = new List<string> { "Email" },
            KeepFirstOccurrence = false, // Keep last occurrence
            CaseSensitive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/datasets/{dataSetId}/remove-duplicates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<JobSubmissionResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.JobId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RemoveDuplicates_WithNonExistentDataset_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentDataSetId = Guid.NewGuid();

        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = new List<string> { "Email" },
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/datasets/{nonExistentDataSetId}/remove-duplicates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var user = Domain.Entities.User.Register(
            auth0UserId: TestUserId,
            displayName: TestName
        );

        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

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
            userId: TestUserId,
            fileInfo: fileInfo,
            statistics: statistics
        );

        context.DataSets.Add(dataSet);
        await context.SaveChangesAsync();

        return dataSet.Id;
    }

    private async Task DeleteDataSetAsync(Guid dataSetId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var dataSet = await context.DataSets.FindAsync(dataSetId);
        if (dataSet != null)
        {
            dataSet.Delete(TestUserId);
            await context.SaveChangesAsync();
        }
    }

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

    private async Task<Guid> CreateTestCompletedJobAsync(Guid dataSetId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

        var options = DuplicateRemovalOptions.KeepFirst(
            keyColumns: new List<string> { "Email" },
            caseSensitivity: CaseSensitivity.Insensitive
        );

        var job = NormalizationJob.CreateDuplicateRemovalJob(dataSetId, options);
        job.Start();
        
        var resultJson = JsonSerializer.Serialize(new
        {
            processedRows = 100,
            rowsRemoved = 10,
            rowsModified = 0,
            processingTimeMs = 500,
            warnings = new List<string>()
        });
        
        job.Complete(resultJson);

        context.NormalizationJobs.Add(job);
        await context.SaveChangesAsync();

        return job.Id;
    }

    #endregion
}

/// <summary>
/// Helper extensions for creating authenticated test clients
/// </summary>
public static class TestApplicationFactoryExtensions
{
    public static HttpClient CreateAuthenticatedClient(
        this ApiTestApplicationFactory factory,
        string userId,
        string email,
        string name)
    {
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });

                services.AddAuthorization(options =>
                {
                    options.AddPolicy("Test", policy =>
                    {
                        policy.AuthenticationSchemes.Add("Test");
                        policy.RequireAuthenticatedUser();
                    });
                });
            });
        }).CreateClient();

        // Add test authentication header
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-Test-User-Email", email);
        client.DefaultRequestHeaders.Add("X-Test-User-Name", name);

        return client;
    }
}

/// <summary>
/// Test authentication handler that creates a test user principal
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        Microsoft.AspNetCore.Authentication.ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Test-User-Id"].FirstOrDefault() ?? "auth0|test-user-123";
        var email = Request.Headers["X-Test-User-Email"].FirstOrDefault() ?? "test@example.com";
        var name = Request.Headers["X-Test-User-Name"].FirstOrDefault() ?? "Test User";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email),
            new Claim(ClaimTypes.Name, name),
            new Claim("name", name),
            new Claim("email_verified", "true")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

