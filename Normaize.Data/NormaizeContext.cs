using Microsoft.EntityFrameworkCore;
using Normaize.Core.Models;
using Normaize.Core.DTOs;

namespace Normaize.Data;

public class NormaizeContext : DbContext
{
    public NormaizeContext(DbContextOptions<NormaizeContext> options) : base(options)
    {
    }

    public DbSet<DataSet> DataSets { get; set; }
    public DbSet<Analysis> Analyses { get; set; }
    public DbSet<DataSetRow> DataSetRows { get; set; }
    public DbSet<DataSetAuditLog> DataSetAuditLogs { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }

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
            entity.Property(e => e.FileType).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StorageProvider).HasConversion<string>().HasMaxLength(50).HasDefaultValue(StorageProvider.Local);
            entity.Property(e => e.UploadedAt);
            entity.Property(e => e.IsProcessed).HasDefaultValue(false);
            entity.Property(e => e.UseSeparateTable).HasDefaultValue(false);

            // Soft delete properties
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt);
            entity.Property(e => e.DeletedBy).HasMaxLength(255);

            // Audit trail properties
            entity.Property(e => e.LastModifiedAt);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(255);

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
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt }).HasDatabaseName("idx_datasets_soft_delete");

            // Note: JSON indexes require generated columns in MySQL, removed for simplicity
        });

        // DataSetRow configuration
        modelBuilder.Entity<DataSetRow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired().HasColumnType("JSON");
            entity.Property(e => e.CreatedAt);

            entity.HasOne(e => e.DataSet)
                  .WithMany(d => d.Rows)
                  .HasForeignKey(e => e.DataSetId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Composite index for performance
            entity.HasIndex(e => new { e.DataSetId, e.RowIndex }).HasDatabaseName("idx_datasetrow_dataset_row");

            // Note: JSON indexes require generated columns in MySQL, removed for simplicity
        });

        // Analysis configuration
        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasConversion<string>().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50).HasDefaultValue(AnalysisStatus.Pending);
            entity.Property(e => e.CreatedAt);

            // Soft delete properties
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt);
            entity.Property(e => e.DeletedBy).HasMaxLength(255);

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
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt }).HasDatabaseName("idx_analyses_soft_delete");
        });

        // DataSetAuditLog configuration
        modelBuilder.Entity<DataSetAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Changes).HasColumnType("JSON");
            entity.Property(e => e.Timestamp);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnType("TEXT");

            entity.HasOne(e => e.DataSet)
                  .WithMany(d => d.AuditLogs)
                  .HasForeignKey(e => e.DataSetId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for audit trail queries
            entity.HasIndex(e => e.DataSetId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.DataSetId, e.Timestamp }).HasDatabaseName("idx_audit_dataset_timestamp");
        });

        // UserSettings configuration
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);

            // Notification settings
            entity.Property(e => e.EmailNotificationsEnabled).HasDefaultValue(true);
            entity.Property(e => e.PushNotificationsEnabled).HasDefaultValue(true);
            entity.Property(e => e.ProcessingCompleteNotifications).HasDefaultValue(true);
            entity.Property(e => e.ErrorNotifications).HasDefaultValue(true);
            entity.Property(e => e.WeeklyDigestEnabled).HasDefaultValue(false);

            // UI/UX preferences
            entity.Property(e => e.Theme).HasMaxLength(20).HasDefaultValue("light");
            entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("en");
            entity.Property(e => e.DefaultPageSize).HasDefaultValue(20);
            entity.Property(e => e.ShowTutorials).HasDefaultValue(true);
            entity.Property(e => e.CompactMode).HasDefaultValue(false);

            // Data processing preferences
            entity.Property(e => e.AutoProcessUploads).HasDefaultValue(true);
            entity.Property(e => e.MaxPreviewRows).HasDefaultValue(100);
            entity.Property(e => e.DefaultFileType).HasMaxLength(20).HasDefaultValue("CSV");
            entity.Property(e => e.EnableDataValidation).HasDefaultValue(true);
            entity.Property(e => e.EnableSchemaInference).HasDefaultValue(true);

            // Privacy settings
            entity.Property(e => e.ShareAnalytics).HasDefaultValue(true);
            entity.Property(e => e.AllowDataUsageForImprovement).HasDefaultValue(false);
            entity.Property(e => e.ShowProcessingTime).HasDefaultValue(true);

            // Account information
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.TimeZone).HasMaxLength(50).HasDefaultValue("UTC");
            entity.Property(e => e.DateFormat).HasMaxLength(20).HasDefaultValue("MM/dd/yyyy");
            entity.Property(e => e.TimeFormat).HasMaxLength(10).HasDefaultValue("12h");

            // Timestamps
            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.UpdatedAt);

            // Soft delete
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt);

            // Indexes
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.UpdatedAt);
        });
    }
}