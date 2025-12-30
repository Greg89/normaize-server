using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of Statistics repository
/// </summary>
public class StatisticsRepository : IStatisticsRepository
{
    private readonly DataNormalizationDbContext _context;
    private readonly ILogger<StatisticsRepository> _logger;

    public StatisticsRepository(
        DataNormalizationDbContext context,
        ILogger<StatisticsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Statistics> AddAsync(Statistics statistics, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding statistics for DataSet {DataSetId}", statistics.DataSetId);

        try
        {
            var entity = _context.Statistics.Add(statistics);
            await _context.SaveChangesAsync(cancellationToken);

            // Set the generated ID
            statistics.SetId(entity.Entity.Id);

            _logger.LogInformation("Successfully added statistics with ID {StatisticsId}", statistics.Id);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding statistics for DataSet {DataSetId}", statistics.DataSetId);
            throw;
        }
    }

    public async Task<Statistics?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting statistics by ID {StatisticsId}", id);

        try
        {
            var statistics = await _context.Statistics
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

            if (statistics != null)
            {
                _logger.LogInformation("Found statistics {StatisticsId}", id);
            }
            else
            {
                _logger.LogInformation("Statistics {StatisticsId} not found", id);
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics {StatisticsId}", id);
            throw;
        }
    }

    public async Task<Statistics?> GetByDataSetIdAsync(Guid dataSetId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting statistics by DataSet ID {DataSetId}", dataSetId);

        try
        {
            var statistics = await _context.Statistics
                .Where(s => s.DataSetId == dataSetId && !s.IsDeleted)
                .OrderByDescending(s => s.CalculatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (statistics != null)
            {
                _logger.LogInformation("Found statistics for DataSet {DataSetId}", dataSetId);
            }
            else
            {
                _logger.LogInformation("No statistics found for DataSet {DataSetId}", dataSetId);
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for DataSet {DataSetId}", dataSetId);
            throw;
        }
    }

    public async Task<IEnumerable<Statistics>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting statistics by User ID {UserId}", userId);

        try
        {
            // Join with DataSet to filter by user
            var statistics = await _context.Statistics
                .Join(_context.DataSets,
                    s => s.DataSetId,
                    d => d.Id,
                    (s, d) => new { Statistics = s, DataSet = d })
                .Where(x => x.DataSet.UserId == userId && !x.Statistics.IsDeleted && !x.DataSet.IsDeleted)
                .Select(x => x.Statistics)
                .OrderByDescending(s => s.CalculatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} statistics records for user {UserId}", statistics.Count, userId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Statistics> UpdateAsync(Statistics statistics, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating statistics {StatisticsId}", statistics.Id);

        try
        {
            _context.Statistics.Update(statistics);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated statistics {StatisticsId}", statistics.Id);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating statistics {StatisticsId}", statistics.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting statistics {StatisticsId}", id);

        try
        {
            var statistics = await GetByIdAsync(id, cancellationToken);
            if (statistics != null)
            {
                statistics.Delete("System"); // In real scenario, would pass actual user
                await UpdateAsync(statistics, cancellationToken);
                _logger.LogInformation("Successfully deleted statistics {StatisticsId}", id);
            }
            else
            {
                _logger.LogWarning("Statistics {StatisticsId} not found for deletion", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting statistics {StatisticsId}", id);
            throw;
        }
    }

    public async Task DeleteByDataSetIdAsync(Guid dataSetId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting statistics for DataSet {DataSetId}", dataSetId);

        try
        {
            var statistics = await _context.Statistics
                .Where(s => s.DataSetId == dataSetId && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var stat in statistics)
            {
                stat.Delete("System"); // In real scenario, would pass actual user
            }

            if (statistics.Any())
            {
                _context.Statistics.UpdateRange(statistics);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully deleted {Count} statistics for DataSet {DataSetId}",
                    statistics.Count, dataSetId);
            }
            else
            {
                _logger.LogInformation("No statistics found to delete for DataSet {DataSetId}", dataSetId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting statistics for DataSet {DataSetId}", dataSetId);
            throw;
        }
    }

    public async Task<bool> ExistsForDataSetAsync(Guid dataSetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _context.Statistics
                .AnyAsync(s => s.DataSetId == dataSetId && !s.IsDeleted, cancellationToken);

            _logger.LogInformation("Statistics exist for DataSet {DataSetId}: {Exists}", dataSetId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if statistics exist for DataSet {DataSetId}", dataSetId);
            throw;
        }
    }
}