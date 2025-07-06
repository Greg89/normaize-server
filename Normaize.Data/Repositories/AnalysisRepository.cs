using Microsoft.EntityFrameworkCore;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data;

namespace Normaize.Data.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly NormaizeContext _context;

    public AnalysisRepository(NormaizeContext context)
    {
        _context = context;
    }

    public async Task<Analysis?> GetByIdAsync(int id)
    {
        return await _context.Analyses
            .Include(a => a.DataSet)
            .Include(a => a.ComparisonDataSet)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Analysis>> GetByDataSetIdAsync(int dataSetId)
    {
        return await _context.Analyses
            .Include(a => a.DataSet)
            .Include(a => a.ComparisonDataSet)
            .Where(a => a.DataSetId == dataSetId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Analysis>> GetAllAsync()
    {
        return await _context.Analyses
            .Include(a => a.DataSet)
            .Include(a => a.ComparisonDataSet)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Analysis> AddAsync(Analysis analysis)
    {
        _context.Analyses.Add(analysis);
        await _context.SaveChangesAsync();
        return analysis;
    }

    public async Task<Analysis> UpdateAsync(Analysis analysis)
    {
        _context.Analyses.Update(analysis);
        await _context.SaveChangesAsync();
        return analysis;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var analysis = await _context.Analyses.FindAsync(id);
        if (analysis == null)
            return false;

        _context.Analyses.Remove(analysis);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Analyses.AnyAsync(a => a.Id == id);
    }
} 