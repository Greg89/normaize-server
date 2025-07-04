using Microsoft.EntityFrameworkCore;
using Normaize.Core.Models;

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
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DataSet>> GetAllAsync()
    {
        return await _context.DataSets
            .Include(d => d.Analyses)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<DataSet> AddAsync(DataSet dataSet)
    {
        _context.DataSets.Add(dataSet);
        await _context.SaveChangesAsync();
        return dataSet;
    }

    public async Task<DataSet> UpdateAsync(DataSet dataSet)
    {
        _context.DataSets.Update(dataSet);
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

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.DataSets.AnyAsync(d => d.Id == id);
    }
} 