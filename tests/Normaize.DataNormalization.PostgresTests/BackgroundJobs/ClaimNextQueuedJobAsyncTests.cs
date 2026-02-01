using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Events;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Repositories;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.PostgresTests.BackgroundJobs;

public sealed class ClaimNextQueuedJobAsyncTests(PostgresContainerFixture fixture) : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [SkippableFact]
    public async Task ClaimNextQueuedJobAsync_ReturnsNull_WhenRowIsLockedByAnotherTransaction()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await CleanupAsync();

        var (dataSetId, jobId) = await SeedOneQueuedJobAsync();

        // Lock the queued job row in a separate transaction (simulates another worker holding the lock).
        await using var lockingConnection = new NpgsqlConnection(_fixture.ConnectionString);
        await lockingConnection.OpenAsync();
        await using var tx = await lockingConnection.BeginTransactionAsync();

        await using (var lockCmd = lockingConnection.CreateCommand())
        {
            lockCmd.Transaction = tx;
            lockCmd.CommandText = "\nSELECT id\nFROM \"data_normalization\".\"normalization_jobs\"\nWHERE \"status\" = 'Queued'\nORDER BY \"created_at\"\nLIMIT 1\nFOR UPDATE;";
            var lockedId = (Guid?)await lockCmd.ExecuteScalarAsync();
            Assert.Equal(jobId, lockedId);
        }

        var repo = CreateRepository();

        // Act: should skip the locked row and return null.
        var claimed = await repo.ClaimNextQueuedJobAsync();

        // Assert
        Assert.Null(claimed);

        // Cleanup lock
        await tx.RollbackAsync();

        // Ensure job is still queued (not modified by the claim attempt).
        await using var verifyContext = CreateDbContext();
        var persisted = await verifyContext.NormalizationJobs.AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.Equal(JobStatus.Queued, persisted.Status);
        Assert.Null(persisted.StartedAt);

        _ = dataSetId; // keeps intent explicit; dataset seeded for FK correctness
    }

    [SkippableFact]
    public async Task ClaimNextQueuedJobAsync_TransitionsJobToProcessing_WhenAvailable()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await CleanupAsync();

        var (_, jobId) = await SeedOneQueuedJobAsync();

        var repo = CreateRepository();

        // Act
        var claimed = await repo.ClaimNextQueuedJobAsync();

        // Assert
        Assert.NotNull(claimed);
        Assert.Equal(jobId, claimed!.Id);
        Assert.Equal(JobStatus.Processing, claimed.Status);
        Assert.NotNull(claimed.StartedAt);

        await using var verifyContext = CreateDbContext();
        var persisted = await verifyContext.NormalizationJobs.AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.Equal(JobStatus.Processing, persisted.Status);
        Assert.NotNull(persisted.StartedAt);
    }

    [SkippableFact]
    public async Task ClaimNextQueuedJobAsync_AllowsOnlyOneConcurrentClaim_WhenSingleQueuedRowExists()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await CleanupAsync();

        var (_, jobId) = await SeedOneQueuedJobAsync();

        await using var context1 = CreateDbContext();
        await using var context2 = CreateDbContext();

        var repo1 = CreateRepository(context1);
        var repo2 = CreateRepository(context2);

        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<NormalizationJob?> ClaimAsync(NormalizationJobRepository repo)
        {
            await start.Task;
            return await repo.ClaimNextQueuedJobAsync();
        }

        var claim1 = ClaimAsync(repo1);
        var claim2 = ClaimAsync(repo2);

        start.TrySetResult(true);

        var results = await Task.WhenAll(claim1, claim2);
        var claimed = results.Where(r => r != null).Select(r => r!).ToList();

        Assert.Single(claimed);
        Assert.Equal(jobId, claimed[0].Id);

        await using var verifyContext = CreateDbContext();
        var persisted = await verifyContext.NormalizationJobs.AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.Equal(JobStatus.Processing, persisted.Status);
        Assert.NotNull(persisted.StartedAt);
    }

    private DataNormalizationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new DataNormalizationDbContext(options);
    }

    private NormalizationJobRepository CreateRepository()
    {
        var context = CreateDbContext();
        return CreateRepository(context);
    }

    private NormalizationJobRepository CreateRepository(DataNormalizationDbContext context)
    {
        var publisher = new NoopDomainEventPublisher();
        var logger = NullLogger<NormalizationJobRepository>.Instance;

        return new NormalizationJobRepository(context, publisher, logger);
    }

    private async Task CleanupAsync()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Keep this surgical: only tables we touch.
        // CASCADE ensures dependent rows are removed if FKs exist.
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "TRUNCATE TABLE \"data_normalization\".\"normalization_audit_logs\", \"data_normalization\".\"normalization_jobs\", \"data_normalization\".\"datasets\" RESTART IDENTITY CASCADE;";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<(Guid DataSetId, Guid JobId)> SeedOneQueuedJobAsync()
    {
        await using var context = CreateDbContext();

        var fileInfo = FileMetadata.CreateFromFileName(
            fileName: "file.csv",
            filePath: "s3://test-bucket/file.csv",
            fileSize: 123);

        var dataSet = Normaize.DataNormalization.Domain.Entities.DataSet.Create(
            name: "test dataset",
            description: null,
            userId: "test-user",
            fileInfo: fileInfo,
            statistics: null,
            retentionDays: 30);

        context.DataSets.Add(dataSet);

        var job = NormalizationJob.Create(
            dataSetId: dataSet.Id,
            operationType: "RemoveDuplicates",
            operationParameters: "{}",
            maxRetries: 3);

        context.NormalizationJobs.Add(job);

        await context.SaveChangesAsync();

        // Ensure queued
        Assert.Equal(JobStatus.Queued, job.Status);

        return (dataSet.Id, job.Id);
    }

    private sealed class NoopDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync(Normaize.DataNormalization.Domain.Events.IDomainEvent domainEvent) => Task.CompletedTask;
    }
}
