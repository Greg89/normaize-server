using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of DataSet repository
/// </summary>
public class DataSetRepository : IDataSetRepository
{
    private readonly DataNormalizationDbContext _context;
    private readonly ILogger<DataSetRepository> _logger;

    public DataSetRepository(
        DataNormalizationDbContext context,
        ILogger<DataSetRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DataSet?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Getting dataset by ID: {DataSetId}", id);

        return await _context.DataSets
            .Where(ds => ds.Id == id && !ds.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<DataSet?> GetByIdWithRowsAsync(Guid id)
    {
        _logger.LogDebug("Getting dataset with rows by ID: {DataSetId}", id);

        return await _context.DataSets
            .Where(ds => ds.Id == id && !ds.IsDeleted)
            .FirstOrDefaultAsync();

        // Note: We don't include rows here as they're managed separately
        // This keeps the query performant for large datasets
    }

    public async Task<IEnumerable<DataSet>> GetByUserIdAsync(string userId)
    {
        _logger.LogDebug("Getting datasets for user: {UserId}", userId);

        return await _context.DataSets
            .Where(ds => ds.UserId == userId && !ds.IsDeleted)
            .OrderByDescending(ds => ds.UploadedAt)
            .ToListAsync();
    }

    public async Task<DataSet> SaveAsync(DataSet dataSet)
    {
        _logger.LogDebug("Saving dataset: {DataSetId}", dataSet.Id);

        await _context.DataSets.AddAsync(dataSet);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully saved dataset: {DataSetId}", dataSet.Id);
        return dataSet;
    }

    public async Task<DataSet> UpdateAsync(DataSet dataSet)
    {
        _logger.LogDebug("Updating dataset: {DataSetId}", dataSet.Id);

        _context.DataSets.Update(dataSet);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated dataset: {DataSetId}", dataSet.Id);
        return dataSet;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting dataset: {DataSetId}", id);

        var dataSet = await GetByIdAsync(id);
        if (dataSet == null)
        {
            _logger.LogWarning("Dataset not found for deletion: {DataSetId}", id);
            return false;
        }

        dataSet.Delete("system"); // Use "system" as deletedBy for repository operations
        await UpdateAsync(dataSet);

        _logger.LogInformation("Successfully deleted dataset: {DataSetId}", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.DataSets
            .AnyAsync(ds => ds.Id == id && !ds.IsDeleted);
    }
}

/// <summary>
/// Entity Framework implementation of DataSetRow repository
/// </summary>
public class DataSetRowRepository : IDataSetRowRepository
{
    private readonly DataNormalizationDbContext _context;
    private readonly ILogger<DataSetRowRepository> _logger;

    public DataSetRowRepository(
        DataNormalizationDbContext context,
        ILogger<DataSetRowRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(Guid dataSetId)
    {
        _logger.LogDebug("Getting all rows for dataset: {DataSetId}", dataSetId);

        return await _context.DataSetRows
            .Where(row => row.DataSetId == dataSetId)
            .OrderBy(row => row.RowIndex)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(Guid dataSetId, int skip, int take)
    {
        _logger.LogDebug("Getting rows for dataset: {DataSetId}, skip: {Skip}, take: {Take}", dataSetId, skip, take);

        return await _context.DataSetRows
            .Where(row => row.DataSetId == dataSetId)
            .OrderBy(row => row.RowIndex)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<DataSetRow?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Getting row by ID: {RowId}", id);

        return await _context.DataSetRows
            .FirstOrDefaultAsync(row => row.Id == id);
    }

    public async Task<DataSetRow> SaveAsync(DataSetRow dataSetRow)
    {
        _logger.LogDebug("Saving row: {RowId} for dataset: {DataSetId}", dataSetRow.Id, dataSetRow.DataSetId);

        await _context.DataSetRows.AddAsync(dataSetRow);
        await _context.SaveChangesAsync();

        return dataSetRow;
    }

    public async Task<IEnumerable<DataSetRow>> SaveRangeAsync(IEnumerable<DataSetRow> dataSetRows)
    {
        var rowList = dataSetRows.ToList();
        _logger.LogDebug("Saving {Count} rows", rowList.Count);

        await _context.DataSetRows.AddRangeAsync(rowList);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully saved {Count} rows", rowList.Count);
        return rowList;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting row: {RowId}", id);

        var row = await GetByIdAsync(id);
        if (row == null)
        {
            _logger.LogWarning("Row not found for deletion: {RowId}", id);
            return false;
        }

        _context.DataSetRows.Remove(row);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted row: {RowId}", id);
        return true;
    }

    public async Task<bool> DeleteByDataSetIdAsync(Guid dataSetId)
    {
        _logger.LogDebug("Deleting all rows for dataset: {DataSetId}", dataSetId);

        var rows = await _context.DataSetRows
            .Where(row => row.DataSetId == dataSetId)
            .ToListAsync();

        if (rows.Any())
        {
            _context.DataSetRows.RemoveRange(rows);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted {Count} rows for dataset: {DataSetId}", rows.Count, dataSetId);
        }

        return true;
    }

    public async Task<int> GetCountByDataSetIdAsync(Guid dataSetId)
    {
        return await _context.DataSetRows
            .CountAsync(row => row.DataSetId == dataSetId);
    }
}