using Microsoft.EntityFrameworkCore;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
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
            .Include(d => d.Analyses)
            .Include(d => d.Rows.Take(100)) // Load first 100 rows for preview
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DataSet>> GetAllAsync()
    {
        return await _context.DataSets
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSet>> GetByUserIdAsync(string userId)
    {
        return await _context.DataSets
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<DataSet> AddAsync(DataSet dataSet)
    {
        _context.DataSets.Add(dataSet);
        await _context.SaveChangesAsync();
        return dataSet;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dataSet = await _context.DataSets.FindAsync(id);
        if (dataSet == null)
            return false;

        _context.DataSets.Remove(dataSet);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DataSet> UpdateAsync(DataSet dataSet)
    {
        _context.DataSets.Update(dataSet);
        await _context.SaveChangesAsync();
        return dataSet;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.DataSets.AnyAsync(d => d.Id == id);
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
    public async Task BulkInsertDataRowsAsync(int dataSetId, IEnumerable<DataSetRow> rows)
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
    public async Task<object> GetDataSetStatisticsAsync(int dataSetId)
    {
        var dataSet = await GetByIdAsync(dataSetId);
        if (dataSet == null) return null;

        if (!dataSet.UseSeparateTable && !string.IsNullOrEmpty(dataSet.ProcessedData))
        {
            // Analyze inline data
            var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);
            return AnalyzeData(data);
        }
        else
        {
            // Analyze separate table data
            var rows = await _context.DataSetRows
                .Where(r => r.DataSetId == dataSetId)
                .Take(1000) // Sample for analysis
                .ToListAsync();

            var data = rows.Select(r => JsonSerializer.Deserialize<Dictionary<string, object>>(r.Data)).ToList();
            return AnalyzeData(data);
        }
    }

    private object AnalyzeData(List<Dictionary<string, object>> data)
    {
        if (!data.Any()) return new { };

        var columns = data.First().Keys.ToList();
        var analysis = new Dictionary<string, object>();

        foreach (var column in columns)
        {
            var values = data.Select(row => row.ContainsKey(column) ? row[column]?.ToString() : null)
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToList();

            analysis[column] = new
            {
                Count = values.Count,
                UniqueCount = values.Distinct().Count(),
                NullCount = data.Count - values.Count,
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
            .Where(d => d.UploadedAt < cutoffDate)
            .ToListAsync();

        _context.DataSets.RemoveRange(oldDataSets);
        await _context.SaveChangesAsync();
        return oldDataSets.Count;
    }
} 