using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Repositories;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Repositories;

public class NormalizationJobRepositoryTests : IDisposable
{
    private readonly DataNormalizationDbContext _context;
    private readonly NormalizationJobRepository _repository;
    private readonly TestDomainEventPublisher _eventPublisher;

    public NormalizationJobRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataNormalizationDbContext(options);
        _eventPublisher = new TestDomainEventPublisher();
        _repository = new NormalizationJobRepository(
            _context,
            _eventPublisher,
            NullLogger<NormalizationJobRepository>.Instance);

        // Ensure database is created
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveAsync_ShouldSaveJobAndPublishEvents()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        
        // Create a test dataset using the factory method
        var fileInfo = FileMetadata.Create("test.csv", "/test/path", FileType.CSV, 1024);
        var statistics = DatasetStatistics.Create(100, 10);
        var dataSet = DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);
        
        // Override the Id to match our test
        typeof(DataSet).GetProperty("Id")!.SetValue(dataSet, dataSetId);

        await _context.DataSets.AddAsync(dataSet);
        await _context.SaveChangesAsync();

        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");

        // Act
        await _repository.SaveAsync(job);

        // Assert
        var savedJob = await _context.NormalizationJobs.FindAsync(job.Id);
        Assert.NotNull(savedJob);
        Assert.Equal(job.Id, savedJob.Id);
        Assert.Equal(JobStatus.Queued, savedJob.Status);

        // Verify domain events were published
        Assert.Single(_eventPublisher.PublishedEvents);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public class TestDomainEventPublisher : IDomainEventPublisher
{
    public List<IDomainEvent> PublishedEvents { get; } = new();

    public Task PublishAsync(IDomainEvent domainEvent)
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
