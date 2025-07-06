using Microsoft.EntityFrameworkCore;
using Normaize.Core.Models;

namespace Normaize.Data;

public class NormaizeContext : DbContext
{
    public NormaizeContext(DbContextOptions<NormaizeContext> options) : base(options)
    {
    }

    public DbSet<DataSet> DataSets { get; set; }
    public DbSet<Analysis> Analyses { get; set; }
    public DbSet<DataSetRow> DataSetRows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DataSet configuration
        modelBuilder.Entity<DataSet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StorageProvider).HasMaxLength(50).HasDefaultValue("Local");
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsProcessed).HasDefaultValue(false);
            entity.Property(e => e.UseSeparateTable).HasDefaultValue(false);
            
            // MySQL JSON fields for better performance
            entity.Property(e => e.ProcessedData).HasColumnType("JSON");
            entity.Property(e => e.PreviewData).HasColumnType("JSON");
            entity.Property(e => e.Schema).HasColumnType("JSON");
            entity.Property(e => e.ProcessingErrors).HasColumnType("TEXT");
            entity.Property(e => e.DataHash).HasMaxLength(255);
            
            // Indexes for performance
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.UseSeparateTable);
            
            // JSON indexes for MySQL 8.0+
            entity.HasIndex(e => e.Schema).HasDatabaseName("idx_dataset_schema");
        });

        // DataSetRow configuration
        modelBuilder.Entity<DataSetRow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired().HasColumnType("JSON");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.DataSet)
                  .WithMany(d => d.Rows)
                  .HasForeignKey(e => e.DataSetId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Composite index for performance
            entity.HasIndex(e => new { e.DataSetId, e.RowIndex }).HasDatabaseName("idx_datasetrow_dataset_row");
            
            // JSON index for data field
            entity.HasIndex(e => e.Data).HasDatabaseName("idx_datasetrow_data");
        });

        // Analysis configuration
        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // MySQL JSON fields
            entity.Property(e => e.Configuration).HasColumnType("JSON");
            entity.Property(e => e.Results).HasColumnType("JSON");
            entity.Property(e => e.ErrorMessage).HasColumnType("TEXT");
            
            entity.HasOne(e => e.DataSet)
                  .WithMany(d => d.Analyses)
                  .HasForeignKey(e => e.DataSetId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.ComparisonDataSet)
                  .WithMany()
                  .HasForeignKey(e => e.ComparisonDataSetId)
                  .OnDelete(DeleteBehavior.SetNull);
            
            // Indexes
            entity.HasIndex(e => e.DataSetId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
} 