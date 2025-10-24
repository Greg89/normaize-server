using Microsoft.EntityFrameworkCore;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data.Configurations;

namespace Normaize.DataNormalization.Infrastructure.Data;

/// <summary>
/// DbContext for the Data Normalization bounded context
/// </summary>
public class DataNormalizationDbContext : DbContext
{
    public DataNormalizationDbContext(DbContextOptions<DataNormalizationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<NormalizationJob> NormalizationJobs { get; set; } = null!;
    public DbSet<DataSet> DataSets { get; set; } = null!;
    public DbSet<NormalizationAuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new NormalizationJobConfiguration());
        modelBuilder.ApplyConfiguration(new DataSetConfiguration());
        modelBuilder.ApplyConfiguration(new NormalizationAuditLogConfiguration());

        // Configure schema
        modelBuilder.HasDefaultSchema("data_normalization");
    }
}
