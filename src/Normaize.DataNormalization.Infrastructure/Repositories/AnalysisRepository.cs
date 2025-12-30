using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Services;

namespace Normaize.DataNormalization.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IAnalysisRepository with domain event publishing
/// </summary>
public class AnalysisRepository : IAnalysisRepository
{
    private readonly DataNormalizationDbContext _context;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<AnalysisRepository> _logger;

    public AnalysisRepository(
        DataNormalizationDbContext context,
        IDomainEventPublisher eventPublisher,
        ILogger<AnalysisRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Analysis?> GetByIdAsync(AnalysisId id)
    {
        try
        {
            _logger.LogDebug("Retrieving analysis with ID: {AnalysisId}", id.Value);

            var analysis = await _context.Analyses
                .Include(a => a.DataSet)
                .Include(a => a.ComparisonDataSet)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (analysis == null)
            {
                _logger.LogWarning("Analysis with ID {AnalysisId} not found", id.Value);
            }
            else
            {
                _logger.LogDebug("Successfully retrieved analysis {AnalysisId} with status {Status}",
                    id.Value, analysis.Status);
            }

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis with ID: {AnalysisId}", id.Value);
            throw;
        }
    }

    public async Task<IEnumerable<Analysis>> GetByDataSetIdAsync(Guid dataSetId)
    {
        try
        {
            _logger.LogDebug("Retrieving analyses for dataset: {DataSetId}", dataSetId);

            var analyses = await _context.Analyses
                .Include(a => a.DataSet)
                .Include(a => a.ComparisonDataSet)
                .Where(a => a.DataSetId == dataSetId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {AnalysisCount} analyses for dataset {DataSetId}",
                analyses.Count, dataSetId);

            return analyses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses for dataset: {DataSetId}", dataSetId);
            throw;
        }
    }

    public async Task<IEnumerable<Analysis>> GetByStatusAsync(AnalysisStatus status)
    {
        try
        {
            _logger.LogDebug("Retrieving analyses with status: {Status}", status);

            var analyses = await _context.Analyses
                .Include(a => a.DataSet)
                .Include(a => a.ComparisonDataSet)
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {AnalysisCount} analyses with status {Status}",
                analyses.Count, status);

            return analyses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses with status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<Analysis>> GetByTypeAsync(AnalysisType type)
    {
        try
        {
            _logger.LogDebug("Retrieving analyses with type: {Type}", type);

            var analyses = await _context.Analyses
                .Include(a => a.DataSet)
                .Include(a => a.ComparisonDataSet)
                .Where(a => a.Type == type)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {AnalysisCount} analyses with type {Type}",
                analyses.Count, type);

            return analyses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses with type: {Type}", type);
            throw;
        }
    }

    public async Task<IEnumerable<Analysis>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Retrieving all analyses");

            var analyses = await _context.Analyses
                .Include(a => a.DataSet)
                .Include(a => a.ComparisonDataSet)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {AnalysisCount} analyses", analyses.Count);

            return analyses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all analyses");
            throw;
        }
    }

    public async Task<Analysis> AddAsync(Analysis analysis)
    {
        if (analysis == null) throw new ArgumentNullException(nameof(analysis));

        try
        {
            _logger.LogDebug("Adding new analysis: {AnalysisName} of type {AnalysisType}",
                analysis.Name, analysis.Type);

            // Add the analysis to context
            _context.Analyses.Add(analysis);

            // Save changes to database (this will generate the ID)
            await _context.SaveChangesAsync();

            // Set the generated ID on the domain entity
            var entry = _context.Entry(analysis);
            var generatedId = new AnalysisId((int)entry.Property(e => e.Id).CurrentValue!);
            analysis.SetId(generatedId);

            // Save again to persist any domain events that were raised
            await _context.SaveChangesAsync();

            // Publish domain events after successful save
            await PublishDomainEventsAsync(analysis);

            _logger.LogInformation("Successfully added analysis {AnalysisId}: {AnalysisName}",
                analysis.Id.Value, analysis.Name);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding analysis: {AnalysisName}", analysis.Name);
            throw;
        }
    }

    public async Task<Analysis> UpdateAsync(Analysis analysis)
    {
        if (analysis == null) throw new ArgumentNullException(nameof(analysis));

        try
        {
            _logger.LogDebug("Updating analysis {AnalysisId} with status {Status}",
                analysis.Id.Value, analysis.Status);

            // Verify the analysis exists and get existing entity
            var existingAnalysis = await _context.Analyses
                .FirstOrDefaultAsync(a => a.Id == analysis.Id);

            if (existingAnalysis == null)
            {
                throw new InvalidOperationException($"Analysis with ID {analysis.Id.Value} not found for update");
            }

            // Update the existing entity with new values
            _context.Entry(existingAnalysis).CurrentValues.SetValues(analysis);

            // Save changes to database
            await _context.SaveChangesAsync();

            // Publish domain events after successful update
            await PublishDomainEventsAsync(analysis);

            _logger.LogInformation("Successfully updated analysis {AnalysisId}", analysis.Id.Value);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating analysis {AnalysisId}", analysis.Id.Value);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(AnalysisId id, string deletedBy)
    {
        try
        {
            _logger.LogDebug("Soft deleting analysis {AnalysisId} by {DeletedBy}", id.Value, deletedBy);

            var analysis = await _context.Analyses
                .FirstOrDefaultAsync(a => a.Id == id);

            if (analysis == null)
            {
                _logger.LogWarning("Analysis with ID {AnalysisId} not found for deletion", id.Value);
                return false;
            }

            // Perform soft delete through domain method
            analysis.Delete(deletedBy);

            // Save changes
            await _context.SaveChangesAsync();

            // Publish domain events after successful deletion
            await PublishDomainEventsAsync(analysis);

            _logger.LogInformation("Successfully soft deleted analysis {AnalysisId}", id.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting analysis {AnalysisId}", id.Value);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(AnalysisId id)
    {
        try
        {
            var exists = await _context.Analyses
                .AnyAsync(a => a.Id == id);

            _logger.LogDebug("Analysis {AnalysisId} exists: {Exists}", id.Value, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if analysis exists: {AnalysisId}", id.Value);
            throw;
        }
    }

    public async Task<IEnumerable<Analysis>> GetByCriteriaAsync(
        Guid? dataSetId = null,
        AnalysisStatus? status = null,
        AnalysisType? type = null,
        bool includeDeleted = false)
    {
        try
        {
            _logger.LogDebug("Retrieving analyses by criteria - DataSetId: {DataSetId}, Status: {Status}, Type: {Type}, IncludeDeleted: {IncludeDeleted}",
                dataSetId, status, type, includeDeleted);

            var query = _context.Analyses
                .Include(a => a.DataSet)
                .Include(a => a.ComparisonDataSet)
                .AsQueryable();

            // Apply filters
            if (dataSetId.HasValue)
                query = query.Where(a => a.DataSetId == dataSetId.Value);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            if (type.HasValue)
                query = query.Where(a => a.Type == type.Value);

            if (includeDeleted)
                query = query.IgnoreQueryFilters(); // Include soft-deleted records

            // Order by creation date (newest first)
            query = query.OrderByDescending(a => a.CreatedAt);

            var analyses = await query.ToListAsync();

            _logger.LogDebug("Retrieved {AnalysisCount} analyses matching criteria", analyses.Count);

            return analyses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyses by criteria");
            throw;
        }
    }

    /// <summary>
    /// Publishes all domain events for the aggregate and clears them
    /// </summary>
    private async Task PublishDomainEventsAsync(Analysis analysis)
    {
        if (!analysis.DomainEvents.Any())
        {
            return;
        }

        _logger.LogDebug("Publishing {EventCount} domain events for analysis {AnalysisId}",
            analysis.DomainEvents.Count, analysis.Id.Value);

        foreach (var domainEvent in analysis.DomainEvents)
        {
            try
            {
                await _eventPublisher.PublishAsync(domainEvent);
                _logger.LogDebug("Published domain event {EventType} for analysis {AnalysisId}",
                    domainEvent.GetType().Name, analysis.Id.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing domain event {EventType} for analysis {AnalysisId}",
                    domainEvent.GetType().Name, analysis.Id.Value);
                // Continue publishing other events even if one fails
            }
        }

        // Clear events after publishing
        analysis.ClearDomainEvents();
    }
}