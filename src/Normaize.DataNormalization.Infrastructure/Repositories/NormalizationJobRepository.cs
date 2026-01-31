using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Services;

namespace Normaize.DataNormalization.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of INormalizationJobRepository with domain event publishing
/// </summary>
public class NormalizationJobRepository : INormalizationJobRepository
{
    private readonly DataNormalizationDbContext _context;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<NormalizationJobRepository> _logger;

    public NormalizationJobRepository(
        DataNormalizationDbContext context,
        IDomainEventPublisher eventPublisher,
        ILogger<NormalizationJobRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NormalizationJob?> GetByIdAsync(Guid jobId)
    {
        try
        {
            _logger.LogDebug("Retrieving normalization job with ID: {JobId}", jobId);

            var job = await _context.NormalizationJobs
                .Include(j => j.DataSet)
                .Include(j => j.AuditLogs)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                _logger.LogWarning("Normalization job with ID {JobId} not found", jobId);
            }
            else
            {
                _logger.LogDebug("Successfully retrieved normalization job {JobId} with status {Status}",
                    jobId, job.Status);
            }

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving normalization job with ID: {JobId}", jobId);
            throw;
        }
    }

    public async Task<NormalizationJob?> GetNextQueuedJobAsync()
    {
        try
        {
            _logger.LogDebug("Retrieving next queued normalization job");

            var job = await _context.NormalizationJobs
                .Include(j => j.DataSet)
                .Include(j => j.AuditLogs)
                .Where(j => j.Status == JobStatus.Queued)
                .OrderBy(j => j.CreatedAt)
                .FirstOrDefaultAsync();

            if (job == null)
            {
                _logger.LogDebug("No queued normalization jobs found");
            }
            else
            {
                _logger.LogDebug("Retrieved next queued job: {JobId}, created at {CreatedAt}",
                    job.Id, job.CreatedAt);
            }

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving next queued normalization job");
            throw;
        }
    }

    public async Task<(IReadOnlyList<NormalizationJob> Jobs, int TotalItems)> GetJobsForUserAsync(
        string userId,
        Guid? dataSetId = null,
        JobStatus? status = null,
        string? operationType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            if (pageNumber < 1)
                throw new ArgumentException("PageNumber must be >= 1", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("PageSize must be between 1 and 100", nameof(pageSize));

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
                throw new ArgumentException("StartDate must be <= EndDate");

            var query = _context.NormalizationJobs
                .AsNoTracking()
                .Include(j => j.DataSet)
                .Where(j => j.DataSet != null && j.DataSet.UserId == userId);

            if (dataSetId.HasValue)
            {
                query = query.Where(j => j.DataSetId == dataSetId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(j => j.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(operationType))
            {
                query = query.Where(j => j.OperationType == operationType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(j => j.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(j => j.CreatedAt <= endDate.Value);
            }

            var totalItems = await query.CountAsync();

            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (jobs, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving normalization jobs for user {UserId}", userId);
            throw;
        }
    }

    public async Task SaveAsync(NormalizationJob job)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));

        try
        {
            _logger.LogDebug("Saving normalization job {JobId} with status {Status}",
                job.Id, job.Status);

            // Add the job to context
            _context.NormalizationJobs.Add(job);

            // Save changes to database
            await _context.SaveChangesAsync();

            // Publish domain events after successful save
            await PublishDomainEventsAsync(job);

            _logger.LogInformation("Successfully saved normalization job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving normalization job {JobId}", job.Id);
            throw;
        }
    }

    public async Task UpdateAsync(NormalizationJob job)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));

        try
        {
            _logger.LogDebug("Updating normalization job {JobId} with status {Status}",
                job.Id, job.Status);

            // Verify the job exists and get existing entity
            var existingJob = await _context.NormalizationJobs
                .Include(j => j.AuditLogs)
                .FirstOrDefaultAsync(j => j.Id == job.Id);

            if (existingJob == null)
            {
                throw new InvalidOperationException($"Normalization job with ID {job.Id} not found for update");
            }

            // Update the existing entity with new values
            _context.Entry(existingJob).CurrentValues.SetValues(job);

            // Handle audit logs collection
            if (job.AuditLogs.Any())
            {
                // Add new audit logs that don't exist
                foreach (var auditLog in job.AuditLogs)
                {
                    if (!existingJob.AuditLogs.Any(al => al.Id == auditLog.Id))
                    {
                        existingJob.AuditLogs.Add(auditLog);
                    }
                }
            }

            // Save changes to database
            await _context.SaveChangesAsync();

            // Publish domain events after successful update
            await PublishDomainEventsAsync(job);

            _logger.LogInformation("Successfully updated normalization job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating normalization job {JobId}", job.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid jobId)
    {
        try
        {
            _logger.LogDebug("Deleting normalization job {JobId}", jobId);

            var job = await _context.NormalizationJobs
                .Include(j => j.AuditLogs)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                _logger.LogWarning("Normalization job with ID {JobId} not found for deletion", jobId);
                return;
            }

            // Remove the job (audit logs will be cascade deleted due to foreign key)
            _context.NormalizationJobs.Remove(job);

            // Save changes
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted normalization job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting normalization job {JobId}", jobId);
            throw;
        }
    }

    /// <summary>
    /// Publishes all domain events for the aggregate and clears them
    /// </summary>
    private async Task PublishDomainEventsAsync(NormalizationJob job)
    {
        if (!job.DomainEvents.Any())
        {
            return;
        }

        _logger.LogDebug("Publishing {EventCount} domain events for job {JobId}",
            job.DomainEvents.Count, job.Id);

        foreach (var domainEvent in job.DomainEvents)
        {
            try
            {
                await _eventPublisher.PublishAsync(domainEvent);
                _logger.LogDebug("Published domain event {EventType} for job {JobId}",
                    domainEvent.GetType().Name, job.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing domain event {EventType} for job {JobId}",
                    domainEvent.GetType().Name, job.Id);
                // Continue publishing other events even if one fails
            }
        }

        // Clear events after publishing
        job.ClearDomainEvents();
    }
}
