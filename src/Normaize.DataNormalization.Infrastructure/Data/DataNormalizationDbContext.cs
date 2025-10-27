using Microsoft.EntityFrameworkCore;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data.Configurations;
using Normaize.DataNormalization.Infrastructure.Persistence.Configurations;
using AnalysisConfig = Normaize.DataNormalization.Infrastructure.Data.Configurations.AnalysisConfiguration;

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
    public DbSet<Analysis> Analyses { get; set; } = null!;
    public DbSet<Statistics> Statistics { get; set; } = null!;
    public DbSet<DataSet> DataSets { get; set; } = null!;
    public DbSet<DataSetRow> DataSetRows { get; set; } = null!;
    public DbSet<NormalizationAuditLog> AuditLogs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new NormalizationJobConfiguration());
        modelBuilder.ApplyConfiguration(new AnalysisConfig());
        modelBuilder.ApplyConfiguration(new StatisticsConfiguration());
        modelBuilder.ApplyConfiguration(new DataSetConfiguration());
        modelBuilder.ApplyConfiguration(new DataSetRowConfiguration());
        modelBuilder.ApplyConfiguration(new NormalizationAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // Configure schema
        modelBuilder.HasDefaultSchema("data_normalization");
    }
}
