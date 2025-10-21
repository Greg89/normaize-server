using Microsoft.EntityFrameworkCore;
using Normaize.DataNormalization.Domain.Aggregates;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure NormalizationJob aggregate
        modelBuilder.Entity<NormalizationJob>(entity =>
        {
            entity.ToTable("normalization_jobs", "data_normalization");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.DataSetId)
                .HasColumnName("dataset_id")
                .IsRequired();
            
            entity.Property(e => e.OperationType)
                .HasColumnName("operation_type")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.OperationParameters)
                .HasColumnName("operation_parameters")
                .HasColumnType("jsonb")
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.RetryCount)
                .HasColumnName("retry_count")
                .HasDefaultValue(0);
            
            entity.Property(e => e.MaxRetries)
                .HasColumnName("max_retries")
                .HasDefaultValue(5);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            
            entity.Property(e => e.StartedAt)
                .HasColumnName("started_at");
            
            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_at");
            
            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(1000);
            
            entity.Property(e => e.Result)
                .HasColumnName("result")
                .HasColumnType("jsonb");
            
            entity.Property(e => e.ProgressPercentage)
                .HasColumnName("progress_percentage")
                .HasDefaultValue(0);
            
            entity.Property(e => e.ProgressMessage)
                .HasColumnName("progress_message")
                .HasMaxLength(500);

            // Indexes for performance
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_normalization_jobs_status");
            
            entity.HasIndex(e => e.DataSetId)
                .HasDatabaseName("ix_normalization_jobs_dataset_id");
            
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_normalization_jobs_created_at");
            
            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("ix_normalization_jobs_status_created_at");
        });

        // Configure schema
        modelBuilder.HasDefaultSchema("data_normalization");
    }
}
