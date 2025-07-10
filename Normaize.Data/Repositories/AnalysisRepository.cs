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
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
    }

    public async Task<IEnumerable<Analysis>> GetByDataSetIdAsync(int dataSetId)
    {
        return await _context.Analyses
            .Include(a => a.DataSet)
            .Include(a => a.ComparisonDataSet)
            .Where(a => a.DataSetId == dataSetId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Analysis>> GetAllAsync()
    {
        return await _context.Analyses
            .Include(a => a.DataSet)
            .Include(a => a.ComparisonDataSet)
            .Where(a => !a.IsDeleted)
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
        if (analysis == null || analysis.IsDeleted)
            return false;

        // Soft delete
        analysis.IsDeleted = true;
        analysis.DeletedAt = DateTime.UtcNow;
        analysis.DeletedBy = "System"; // This will be updated by the service layer
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HardDeleteAsync(int id)
    {
        var analysis = await _context.Analyses.FindAsync(id);
        if (analysis == null)
            return false;

        _context.Analyses.Remove(analysis);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var analysis = await _context.Analyses.FindAsync(id);
        if (analysis == null || !analysis.IsDeleted)
            return false;

        // Restore soft deleted record
        analysis.IsDeleted = false;
        analysis.DeletedAt = null;
        analysis.DeletedBy = null;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Analysis>> GetDeletedAsync()
    {
        return await _context.Analyses
            .Where(a => a.IsDeleted)
            .OrderByDescending(a => a.DeletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Analysis>> GetByStatusAsync(string status)
    {
        return await _context.Analyses
            .Include(a => a.DataSet)
            .Include(a => a.ComparisonDataSet)
            .Where(a => a.Status == status && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Analysis>> GetByTypeAsync(string type)
    {
        return await _context.Analyses
            .Include(a => a.DataSet)
            .Include(a => a.ComparisonDataSet)
            .Where(a => a.Type == type && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> PermanentlyDeleteOldSoftDeletedAsync(int daysToKeep)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldSoftDeleted = await _context.Analyses
            .Where(a => a.IsDeleted && a.DeletedAt < cutoffDate)
            .ToListAsync();

        _context.Analyses.RemoveRange(oldSoftDeleted);
        await _context.SaveChangesAsync();
        return oldSoftDeleted.Count;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Analyses.AnyAsync(a => a.Id == id && !a.IsDeleted);
    }
} 