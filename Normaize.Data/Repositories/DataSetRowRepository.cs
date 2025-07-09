using Microsoft.EntityFrameworkCore;
using Normaize.Core.Models;

namespace Normaize.Data.Repositories;

public class DataSetRowRepository : IDataSetRowRepository
{
    private readonly NormaizeContext _context;

    public DataSetRowRepository(NormaizeContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(int dataSetId)
    {
        return await _context.DataSetRows
            .Where(r => r.DataSetId == dataSetId)
            .OrderBy(r => r.RowIndex)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSetRow>> GetByDataSetIdAsync(int dataSetId, int skip, int take)
    {
        return await _context.DataSetRows
            .Where(r => r.DataSetId == dataSetId)
            .OrderBy(r => r.RowIndex)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<DataSetRow?> GetByIdAsync(int id)
    {
        return await _context.DataSetRows.FindAsync(id);
    }

    public async Task<DataSetRow> AddAsync(DataSetRow dataSetRow)
    {
        _context.DataSetRows.Add(dataSetRow);
        await _context.SaveChangesAsync();
        return dataSetRow;
    }

    public async Task<IEnumerable<DataSetRow>> AddRangeAsync(IEnumerable<DataSetRow> dataSetRows)
    {
        _context.DataSetRows.AddRange(dataSetRows);
        await _context.SaveChangesAsync();
        return dataSetRows;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dataSetRow = await _context.DataSetRows.FindAsync(id);
        if (dataSetRow == null)
            return false;

        _context.DataSetRows.Remove(dataSetRow);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByDataSetIdAsync(int dataSetId)
    {
        var rows = await _context.DataSetRows
            .Where(r => r.DataSetId == dataSetId)
            .ToListAsync();

        if (!rows.Any())
            return false;

        _context.DataSetRows.RemoveRange(rows);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountByDataSetIdAsync(int dataSetId)
    {
        return await _context.DataSetRows
            .Where(r => r.DataSetId == dataSetId)
            .CountAsync();
    }
} 