using System;using System;

using System.Collections.Generic;using System.Collections.Generic;

using System.Threading.Tasks;using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Logging.Abstractions;using Microsoft.Extensions.Logging.Abstractions;

using Normaize.DataNormalization.Domain.Aggregates;using Normaize.DataNormalization.Domain.Aggregates;

using Normaize.DataNormalization.Infrastructure.Data;using Normaize.DataNormalization.Domain.Entities;

using Normaize.DataNormalization.Infrastructure.Repositories;using Normaize.DataNormalization.Domain.ValueObjects;

using Normaize.DataNormalization.Infrastructure.Services;using Normaize.DataNormalization.Infrastructure.Data;

using Xunit;using Normaize.DataNormalization.Infrastructure.Repositories;

using Normaize.DataNormalization.Infrastructure.Services;

namespace Normaize.DataNormalization.Infrastructure.Tests.Repositories;using Xunit;



public class NormalizationJobRepositoryTests : IDisposablenamespace Normaize.DataNormalization.Infrastructure.Tests.Repositories;

{

    private readonly DataNormalizationDbContext _context;public class NormalizationJobRepositoryTests : IDisposable

    private readonly NormalizationJobRepository _repository;{

    private readonly TestDomainEventPublisher _eventPublisher;    private readonly DataNormalizationDbContext _context;

    private readonly NormalizationJobRepository _repository;

    public NormalizationJobRepositoryTests()    private readonly TestDomainEventPublisher _eventPublisher;

    {

        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()    public NormalizationJobRepositoryTests()

            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())    {

            .Options;        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()

            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())

        _context = new DataNormalizationDbContext(options);            .Options;

        _eventPublisher = new TestDomainEventPublisher();

        _repository = new NormalizationJobRepository(        _context = new DataNormalizationDbContext(options);

            _context,        _eventPublisher = new TestDomainEventPublisher();

            _eventPublisher,        _repository = new NormalizationJobRepository(

            NullLogger<NormalizationJobRepository>.Instance);            _context,

            _eventPublisher,

        // Ensure database is created            NullLogger<NormalizationJobRepository>.Instance);

        _context.Database.EnsureCreated();

    }        // Ensure database is created

        _context.Database.EnsureCreated();

    [Fact]    }

    public async Task SaveAsync_ShouldSaveJobAndPublishEvents()

    {    [Fact]

        // Arrange    public async Task SaveAsync_ShouldSaveJobAndPublishEvents()

        var dataSetId = Guid.NewGuid();    {

        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");        // Arrange

        var dataSetId = Guid.NewGuid();

        // Act        

        await _repository.SaveAsync(job);        // Create a test dataset using the factory method

        var fileInfo = FileMetadata.Create("test.csv", "/test/path", 1024, StorageProvider.Local, FileType.Csv);

        // Assert        var statistics = DatasetStatistics.Create(100, 10);

        var savedJob = await _context.NormalizationJobs.FindAsync(job.Id);        var dataSet = DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);

        Assert.NotNull(savedJob);        

        Assert.Equal(job.Id, savedJob.Id);        // Override the Id to match our test

        Assert.Equal(JobStatus.Queued, savedJob.Status);        typeof(DataSet).GetProperty("Id")!.SetValue(dataSet, dataSetId);



        // Verify domain events were published        await _context.DataSets.AddAsync(dataSet);

        Assert.Single(_eventPublisher.PublishedEvents);        await _context.SaveChangesAsync();

    }

        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");

    [Fact]

    public async Task GetByIdAsync_ShouldReturnJob()        // Act

    {        await _repository.SaveAsync(job);

        // Arrange

        var dataSetId = Guid.NewGuid();        // Assert

        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");        var savedJob = await _context.NormalizationJobs.FindAsync(job.Id);

        await _context.NormalizationJobs.AddAsync(job);        Assert.NotNull(savedJob);

        await _context.SaveChangesAsync();        Assert.Equal(job.Id, savedJob.Id);

        Assert.Equal(JobStatus.Queued, savedJob.Status);

        // Act

        var result = await _repository.GetByIdAsync(job.Id);        // Verify domain events were published

        Assert.Single(_eventPublisher.PublishedEvents);

        // Assert    }

        Assert.NotNull(result);

        Assert.Equal(job.Id, result.Id);    [Fact]

        Assert.Equal(dataSetId, result.DataSetId);    public async Task GetByIdAsync_ShouldReturnJobWithRelatedEntities()

    }    {

        // Arrange

    [Fact]        var dataSetId = Guid.NewGuid();

    public async Task GetNextQueuedJobAsync_ShouldReturnOldestQueuedJob()        

    {        var fileInfo = FileMetadata.Create("test.csv", "/test/path", 1024, StorageProvider.Local, FileType.Csv);

        // Arrange        var statistics = DatasetStatistics.Create(100, 10);

        var dataSetId = Guid.NewGuid();        var dataSet = DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);

        var job1 = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");        typeof(DataSet).GetProperty("Id")!.SetValue(dataSet, dataSetId);

        var job2 = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");

                await _context.DataSets.AddAsync(dataSet);

        // Make job2 older by setting an earlier creation time        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");

        var earlierTime = DateTime.UtcNow.AddMinutes(-5);        await _context.NormalizationJobs.AddAsync(job);

        typeof(NormalizationJob).GetProperty("CreatedAt")!.SetValue(job2, earlierTime);        await _context.SaveChangesAsync();



        await _context.NormalizationJobs.AddRangeAsync(job1, job2);        // Act

        await _context.SaveChangesAsync();        var result = await _repository.GetByIdAsync(job.Id);



        // Act        // Assert

        var result = await _repository.GetNextQueuedJobAsync();        Assert.NotNull(result);

        Assert.Equal(job.Id, result.Id);

        // Assert        Assert.NotNull(result.DataSet);

        Assert.NotNull(result);        Assert.Equal(dataSetId, result.DataSet.Id);

        Assert.Equal(job2.Id, result.Id); // Should return the older job    }

    }

    [Fact]

    [Fact]    public async Task GetNextQueuedJobAsync_ShouldReturnOldestQueuedJob()

    public async Task UpdateAsync_ShouldUpdateJobAndPublishEvents()    {

    {        // Arrange

        // Arrange        var dataSetId = Guid.NewGuid();

        var dataSetId = Guid.NewGuid();        var fileInfo = FileMetadata.Create("test.csv", "/test/path", 1024, StorageProvider.Local, FileType.Csv);

        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");        var statistics = DatasetStatistics.Create(100, 10, true, false, false);

        await _context.NormalizationJobs.AddAsync(job);        var dataSet = DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);

        await _context.SaveChangesAsync();        typeof(DataSet).GetProperty("Id")!.SetValue(dataSet, dataSetId);



        // Clear initial events        await _context.DataSets.AddAsync(dataSet);

        _eventPublisher.PublishedEvents.Clear();

        var job1 = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");

        // Act        var job2 = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");

        job.Start();        

        await _repository.UpdateAsync(job);        // Make job2 older by setting an earlier creation time

        var earlierTime = DateTime.UtcNow.AddMinutes(-5);

        // Assert        typeof(NormalizationJob).GetProperty("CreatedAt")!.SetValue(job2, earlierTime);

        var updatedJob = await _context.NormalizationJobs.FindAsync(job.Id);

        Assert.NotNull(updatedJob);        await _context.NormalizationJobs.AddRangeAsync(job1, job2);

        Assert.Equal(JobStatus.Processing, updatedJob.Status);        await _context.SaveChangesAsync();

        Assert.NotNull(updatedJob.StartedAt);

        // Act

        // Verify domain events were published        var result = await _repository.GetNextQueuedJobAsync();

        Assert.Single(_eventPublisher.PublishedEvents);

    }        // Assert

        Assert.NotNull(result);

    [Fact]        Assert.Equal(job2.Id, result.Id); // Should return the older job

    public async Task DeleteAsync_ShouldRemoveJob()    }

    {

        // Arrange    [Fact]

        var dataSetId = Guid.NewGuid();    public async Task UpdateAsync_ShouldUpdateJobAndPublishEvents()

        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");    {

        await _context.NormalizationJobs.AddAsync(job);        // Arrange

        await _context.SaveChangesAsync();        var dataSetId = Guid.NewGuid();

        var fileInfo = FileMetadata.Create("test.csv", "/test/path", 1024, FileType.Csv, StorageProvider.Local);

        // Act        var statistics = DatasetStatistics.Create(100, 10, true, false, false);

        await _repository.DeleteAsync(job.Id);        var dataSet = DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);

        typeof(DataSet).GetProperty("Id")!.SetValue(dataSet, dataSetId);

        // Assert

        var deletedJob = await _context.NormalizationJobs.FindAsync(job.Id);        await _context.DataSets.AddAsync(dataSet);

        Assert.Null(deletedJob);        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");

    }        await _context.NormalizationJobs.AddAsync(job);

        await _context.SaveChangesAsync();

    public void Dispose()

    {        // Clear initial events

        _context.Dispose();        _eventPublisher.PublishedEvents.Clear();

    }

}        // Act

        job.Start();

/// <summary>        await _repository.UpdateAsync(job);

/// Test implementation of IDomainEventPublisher for testing

/// </summary>        // Assert

public class TestDomainEventPublisher : IDomainEventPublisher        var updatedJob = await _context.NormalizationJobs.FindAsync(job.Id);

{        Assert.NotNull(updatedJob);

    public List<IDomainEvent> PublishedEvents { get; } = new();        Assert.Equal(JobStatus.Processing, updatedJob.Status);

        Assert.NotNull(updatedJob.StartedAt);

    public Task PublishAsync(IDomainEvent domainEvent)

    {        // Verify domain events were published

        PublishedEvents.Add(domainEvent);        Assert.Single(_eventPublisher.PublishedEvents);

        return Task.CompletedTask;    }

    }

}    [Fact]
    public async Task DeleteAsync_ShouldRemoveJob()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var fileInfo = FileMetadata.Create("test.csv", "/test/path", 1024, FileType.Csv, StorageProvider.Local);
        var statistics = DatasetStatistics.Create(100, 10, true, false, false);
        var dataSet = DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);
        typeof(DataSet).GetProperty("Id")!.SetValue(dataSet, dataSetId);

        await _context.DataSets.AddAsync(dataSet);
        var job = NormalizationJob.Create(dataSetId, "RemoveDuplicates", "{}");
        await _context.NormalizationJobs.AddAsync(job);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(job.Id);

        // Assert
        var deletedJob = await _context.NormalizationJobs.FindAsync(job.Id);
        Assert.Null(deletedJob);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

/// <summary>
/// Test implementation of IDomainEventPublisher for testing
/// </summary>
public class TestDomainEventPublisher : IDomainEventPublisher
{
    public List<IDomainEvent> PublishedEvents { get; } = new();

    public Task PublishAsync(IDomainEvent domainEvent)
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}