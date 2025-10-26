using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for DataSet entity
/// </summary>
public class DataSetConfiguration : IEntityTypeConfiguration<DataSet>
{
    public void Configure(EntityTypeBuilder<DataSet> builder)
    {
        builder.ToTable("datasets", "data_normalization");

        // Primary key
        builder.HasKey(d => d.Id);

        // Scalar properties
        builder.Property(d => d.Id)
            .IsRequired()
            .HasColumnName("id")
            .HasComment("Unique identifier for the dataset");

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("name")
            .HasComment("Human-readable name of the dataset");

        builder.Property(d => d.Description)
            .HasMaxLength(1000)
            .HasColumnName("description")
            .HasComment("Optional description of the dataset");

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450)
            .HasColumnName("user_id")
            .HasComment("ID of the user who owns this dataset");

        builder.Property(d => d.UploadedAt)
            .IsRequired()
            .HasColumnName("uploaded_at")
            .HasComment("When the dataset was uploaded");

        builder.Property(d => d.Schema)
            .HasColumnType("jsonb")
            .HasColumnName("schema")
            .HasComment("JSON schema definition for the dataset");

        builder.Property(d => d.PreviewData)
            .HasColumnType("jsonb")
            .HasColumnName("preview_data")
            .HasComment("Sample data for preview purposes");

        builder.Property(d => d.ProcessedData)
            .HasColumnType("jsonb")
            .HasColumnName("processed_data")
            .HasComment("Processed data for small datasets");

        builder.Property(d => d.ProcessingErrors)
            .HasColumnType("text")
            .HasColumnName("processing_errors")
            .HasComment("Any errors encountered during processing");

        builder.Property(d => d.RetentionExpiryDate)
            .HasColumnName("retention_expiry_date")
            .HasComment("When this dataset should be automatically deleted");

        // Soft delete properties
        builder.Property(d => d.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_deleted")
            .HasComment("Whether this dataset has been soft deleted");

        builder.Property(d => d.DeletedAt)
            .HasColumnName("deleted_at")
            .HasComment("When the dataset was deleted");

        builder.Property(d => d.DeletedBy)
            .HasMaxLength(450)
            .HasColumnName("deleted_by")
            .HasComment("Who deleted the dataset");

        // Audit properties
        builder.Property(d => d.LastModifiedAt)
            .IsRequired()
            .HasColumnName("last_modified_at")
            .HasComment("When the dataset was last modified");

        builder.Property(d => d.LastModifiedBy)
            .HasMaxLength(450)
            .HasColumnName("last_modified_by")
            .HasComment("Who last modified the dataset");

        // Value objects
        builder.OwnsOne(d => d.FileInfo, fileInfo =>
        {
            fileInfo.Property(f => f.FileName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("file_name");

            fileInfo.Property(f => f.FilePath)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("file_path");

            fileInfo.Property(f => f.FileSize)
                .IsRequired()
                .HasColumnName("file_size");

            fileInfo.Property(f => f.DataHash)
                .HasMaxLength(64)
                .HasColumnName("data_hash");

            // FileType stored as string directly
            fileInfo.Property(f => f.FileType)
                .HasConversion(
                    v => v.Value,
                    v => FileType.FromString(v))
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("file_type");

            // StorageProvider stored as string directly  
            fileInfo.Property(f => f.StorageProvider)
                .HasConversion(
                    v => v.Value,
                    v => StorageProvider.FromString(v))
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("storage_provider");
        });

        builder.OwnsOne(d => d.Statistics, stats =>
        {
            stats.Property(s => s.RowCount)
                .IsRequired()
                .HasColumnName("stats_row_count");

            stats.Property(s => s.ColumnCount)
                .IsRequired()
                .HasColumnName("stats_column_count");

            stats.Property(s => s.IsProcessed)
                .IsRequired()
                .HasColumnName("stats_is_processed");

            stats.Property(s => s.UseSeparateTable)
                .IsRequired()
                .HasColumnName("stats_use_separate_table");

            stats.Property(s => s.ProcessedAt)
                .HasColumnName("stats_processed_at");
        });

        builder.OwnsOne(d => d.ProcessingStatus, status =>
        {
            status.Property(s => s.IsProcessed)
                .IsRequired()
                .HasColumnName("processing_is_processed");

            status.Property(s => s.ProcessedAt)
                .HasColumnName("processing_processed_at");

            status.Property(s => s.ProcessingError)
                .HasMaxLength(2000)
                .HasColumnName("processing_error");
        });

        builder.OwnsOne(d => d.RetentionPolicy, retention =>
        {
            retention.Property(r => r.RetentionDays)
                .IsRequired()
                .HasColumnName("retention_days");

            retention.Property(r => r.ExpiryDate)
                .IsRequired()
                .HasColumnName("retention_expiry_date");
        });

        // Ignore computed properties
        builder.Ignore(d => d.RetentionExpiryDate);
        
        // Ignore domain events - they're not persisted
        builder.Ignore(d => d.DomainEvents);

        // Indexes
        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("ix_datasets_user_id");

        builder.HasIndex(d => d.UploadedAt)
            .HasDatabaseName("ix_datasets_uploaded_at");

        builder.HasIndex(d => new { d.IsDeleted, d.UserId })
            .HasDatabaseName("ix_datasets_is_deleted_user_id");

        // Filters
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}

/// <summary>
/// Entity Framework configuration for DataSetRow entity
/// </summary>
public class DataSetRowConfiguration : IEntityTypeConfiguration<DataSetRow>
{
    public void Configure(EntityTypeBuilder<DataSetRow> builder)
    {
        builder.ToTable("dataset_rows", "data_normalization");

        // Primary key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Id)
            .IsRequired()
            .HasColumnName("id")
            .HasComment("Unique identifier for the row");

        builder.Property(r => r.DataSetId)
            .IsRequired()
            .HasColumnName("dataset_id")
            .HasComment("Foreign key to the parent dataset");

        builder.Property(r => r.RowIndex)
            .IsRequired()
            .HasColumnName("row_index")
            .HasComment("Zero-based index of this row within the dataset");

        builder.Property(r => r.Data)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("data")
            .HasComment("JSON representation of the row data");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasComment("When this row was created");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasComment("When this row was last updated");

        // Relationships
        builder.HasOne(r => r.DataSet)
            .WithMany()
            .HasForeignKey(r => r.DataSetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.DataSetId)
            .HasDatabaseName("ix_dataset_rows_dataset_id");

        builder.HasIndex(r => new { r.DataSetId, r.RowIndex })
            .IsUnique()
            .HasDatabaseName("ix_dataset_rows_dataset_id_row_index");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("ix_dataset_rows_created_at");
    }
}