using Microsoft.EntityFrameworkCore;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using Normaize.Data;
using System.Text.Json;

namespace Normaize.Data.Repositories;

public class DataSetRepository : IDataSetRepository
{
    private readonly NormaizeContext _context;

    public DataSetRepository(NormaizeContext context)
    {
        _context = context;
    }

    public async Task<DataSet?> GetByIdAsync(int id)
    {
        return await _context.DataSets
            .Include(d => d.Analyses.Where(a => !a.IsDeleted))
            .Include(d => d.Rows.Take(100)) // Load first 100 rows for preview
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<IEnumerable<DataSet>> GetAllAsync()
    {
        return await _context.DataSets
            .Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetByUserIdAsync(string userId)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<DataSet> AddAsync(DataSet dataSet)
    {
        dataSet.LastModifiedAt = DateTime.UtcNow;
        dataSet.LastModifiedBy = dataSet.UserId;
        
        _context.DataSets.Add(dataSet);
        await _context.SaveChangesAsync();
        return dataSet;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dataSet = await _context.DataSets.FindAsync(id);
        if (dataSet == null || dataSet.IsDeleted)
            return false;

        // Soft delete
        dataSet.IsDeleted = true;
        dataSet.DeletedAt = DateTime.UtcNow;
        dataSet.DeletedBy = dataSet.UserId; // This will be updated by the service layer
        dataSet.LastModifiedAt = DateTime.UtcNow;
        dataSet.LastModifiedBy = dataSet.DeletedBy;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HardDeleteAsync(int id)
    {
        var dataSet = await _context.DataSets.FindAsync(id);
        if (dataSet == null)
            return false;

        _context.DataSets.Remove(dataSet);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var dataSet = await _context.DataSets.FindAsync(id);
        if (dataSet == null || !dataSet.IsDeleted)
            return false;

        // Restore soft deleted record
        dataSet.IsDeleted = false;
        dataSet.DeletedAt = null;
        dataSet.DeletedBy = null;
        dataSet.LastModifiedAt = DateTime.UtcNow;
        dataSet.LastModifiedBy = dataSet.UserId; // This will be updated by the service layer
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DataSet> UpdateAsync(DataSet dataSet)
    {
        dataSet.LastModifiedAt = DateTime.UtcNow;
        dataSet.LastModifiedBy = dataSet.UserId;
        
        _context.DataSets.Update(dataSet);
        await _context.SaveChangesAsync();
        return dataSet;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.DataSets.AnyAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<IEnumerable<DataSet>> GetDeletedAsync()
    {
        return await _context.DataSets
            .Where(d => d.IsDeleted)
            .OrderByDescending(d => d.DeletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetByUserIdAsync(string userId, bool includeDeleted = false)
    {
        var query = _context.DataSets.Where(d => d.UserId == userId);
        
        if (!includeDeleted)
        {
            query = query.Where(d => !d.IsDeleted);
        }
        
        return await query
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> SearchAsync(string searchTerm, string userId)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .Where(d => d.Name.Contains(searchTerm) || 
                       (d.Description != null && d.Description.Contains(searchTerm)) ||
                       d.FileName.Contains(searchTerm))
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetByFileTypeAsync(FileType fileType, string userId)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId && !d.IsDeleted && d.FileType == fileType)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string userId)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId && !d.IsDeleted && 
                       d.UploadedAt >= startDate && d.UploadedAt <= endDate)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<long> GetTotalSizeAsync(string userId)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .SumAsync(d => d.FileSize);
    }

    public async Task<int> GetTotalCountAsync(string userId)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .CountAsync();
    }

    public async Task<IEnumerable<DataSet>> GetRecentlyModifiedAsync(string userId, int count = 10)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .OrderByDescending(d => d.LastModifiedAt)
            .Take(count)
            .ToListAsync();
    }

    // MySQL-specific JSON querying methods
    public async Task<IEnumerable<DataSet>> GetDataSetsBySchemaAsync(string columnName)
    {
        // MySQL JSON query to find datasets containing specific column
        var query = @"
            SELECT * FROM DataSets 
            WHERE JSON_CONTAINS(Schema, '" + JsonSerializer.Serialize(columnName) + @"')
            ORDER BY UploadedAt DESC";

        return await _context.DataSets
            .FromSqlRaw(query)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetDataSetsByDataValueAsync(string columnName, string value)
    {
        // MySQL JSON query to find datasets with specific data values
        var query = @"
            SELECT DISTINCT d.* FROM DataSets d
            WHERE d.UseSeparateTable = 0 
            AND JSON_EXTRACT(d.ProcessedData, '$[*]." + columnName + @"') LIKE '%" + value + @"%'
            ORDER BY d.UploadedAt DESC";

        return await _context.DataSets
            .FromSqlRaw(query)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetDataSetsBySizeRangeAsync(long minSize, long maxSize)
    {
        return await _context.DataSets
            .Where(d => d.FileSize >= minSize && d.FileSize <= maxSize)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.DataSets
            .Where(d => d.UploadedAt >= startDate && d.UploadedAt <= endDate)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    // Bulk operations for large datasets
    public async Task BulkInsertDataRowsAsync(IEnumerable<DataSetRow> rows)
    {
        // Use MySQL bulk insert for better performance
        var batchSize = 1000;
        var batches = rows.Chunk(batchSize);

        foreach (var batch in batches)
        {
            _context.DataSetRows.AddRange(batch);
            await _context.SaveChangesAsync();
        }
    }

    // MySQL-specific data analysis queries
    public async Task<object?> GetDataSetStatisticsAsync(int dataSetId)
    {
        var dataSet = await GetByIdAsync(dataSetId);
        if (dataSet == null) return null;

        if (!dataSet.UseSeparateTable && !string.IsNullOrEmpty(dataSet.ProcessedData))
        {
            // Analyze inline data
            var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);
            return data != null ? AnalyzeData(data.Cast<Dictionary<string, object>?>().ToList()) : null;
        }
        else
        {
            // Analyze separate table data
            var rows = await _context.DataSetRows
                .Where(r => r.DataSetId == dataSetId)
                .Take(1000) // Sample for analysis
                .ToListAsync();

            var data = rows.Select(r => JsonSerializer.Deserialize<Dictionary<string, object>>(r.Data)).ToList();
            return data.Count > 0 ? AnalyzeData(data) : null;
        }
    }

    private static Dictionary<string, object> AnalyzeData(List<Dictionary<string, object>?> data)
    {
        if (data.Count == 0) return [];

        var validData = data.Where(d => d != null).ToList();
        if (validData.Count == 0) return [];

        var columns = validData.First()!.Keys.ToList();
        var analysis = new Dictionary<string, object>();

        foreach (var column in columns)
        {
            var values = validData.Select(row => row!.TryGetValue(column, out var value) ? value?.ToString() : null)
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToList();

            analysis[column] = new
            {
                values.Count,
                UniqueCount = values.Distinct().Count(),
                NullCount = validData.Count - values.Count,
                SampleValues = values.Take(5).ToList()
            };
        }

        return analysis;
    }

    // Cleanup operations
    public async Task<int> CleanupOldDataSetsAsync(int daysToKeep)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldDataSets = await _context.DataSets
            .Where(d => d.UploadedAt < cutoffDate && !d.IsDeleted)
            .ToListAsync();

        foreach (var dataSet in oldDataSets)
        {
            dataSet.IsDeleted = true;
            dataSet.DeletedAt = DateTime.UtcNow;
            dataSet.DeletedBy = "System";
            dataSet.LastModifiedAt = DateTime.UtcNow;
            dataSet.LastModifiedBy = "System";
        }

        await _context.SaveChangesAsync();
        return oldDataSets.Count;
    }

    public async Task<int> PermanentlyDeleteOldSoftDeletedAsync(int daysToKeep)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldSoftDeleted = await _context.DataSets
            .Where(d => d.IsDeleted && d.DeletedAt < cutoffDate)
            .ToListAsync();

        _context.DataSets.RemoveRange(oldSoftDeleted);
        await _context.SaveChangesAsync();
        return oldSoftDeleted.Count;
    }
} 