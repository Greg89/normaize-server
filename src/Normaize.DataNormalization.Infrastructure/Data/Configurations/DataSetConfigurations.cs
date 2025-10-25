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
        builder.ToTable("DataSets", "DataNormalization");

        // Primary key
        builder.HasKey(d => d.Id);

        // Scalar properties
        builder.Property(d => d.Id)
            .IsRequired()
            .HasComment("Unique identifier for the dataset");

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Human-readable name of the dataset");

        builder.Property(d => d.Description)
            .HasMaxLength(1000)
            .HasComment("Optional description of the dataset");

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450)
            .HasComment("ID of the user who owns this dataset");

        builder.Property(d => d.UploadedAt)
            .IsRequired()
            .HasComment("When the dataset was uploaded");

        builder.Property(d => d.Schema)
            .HasColumnType("jsonb")
            .HasComment("JSON schema definition for the dataset");

        builder.Property(d => d.PreviewData)
            .HasColumnType("jsonb")
            .HasComment("Sample data for preview purposes");

        builder.Property(d => d.ProcessedData)
            .HasColumnType("jsonb")
            .HasComment("Processed data for small datasets");

        builder.Property(d => d.ProcessingErrors)
            .HasColumnType("text")
            .HasComment("Any errors encountered during processing");

        builder.Property(d => d.RetentionExpiryDate)
            .HasComment("When this dataset should be automatically deleted");

        // Soft delete properties
        builder.Property(d => d.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this dataset has been soft deleted");

        builder.Property(d => d.DeletedAt)
            .HasComment("When the dataset was deleted");

        builder.Property(d => d.DeletedBy)
            .HasMaxLength(450)
            .HasComment("Who deleted the dataset");

        // Audit properties
        builder.Property(d => d.LastModifiedAt)
            .IsRequired()
            .HasComment("When the dataset was last modified");

        builder.Property(d => d.LastModifiedBy)
            .HasMaxLength(450)
            .HasComment("Who last modified the dataset");

        // Value objects
        builder.OwnsOne(d => d.FileInfo, fileInfo =>
        {
            fileInfo.Property(f => f.FileName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("FileName");

            fileInfo.Property(f => f.FilePath)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("FilePath");

            fileInfo.Property(f => f.FileSize)
                .IsRequired()
                .HasColumnName("FileSize");

            fileInfo.Property(f => f.DataHash)
                .HasMaxLength(64)
                .HasColumnName("DataHash");

            // FileType stored as string directly
            fileInfo.Property(f => f.FileType)
                .HasConversion(
                    v => v.Value,
                    v => FileType.FromString(v))
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("FileType");

            // StorageProvider stored as string directly  
            fileInfo.Property(f => f.StorageProvider)
                .HasConversion(
                    v => v.Value,
                    v => StorageProvider.FromString(v))
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("StorageProvider");
        });

        builder.OwnsOne(d => d.Statistics, stats =>
        {
            stats.Property(s => s.RowCount)
                .IsRequired()
                .HasColumnName("StatsRowCount");

            stats.Property(s => s.ColumnCount)
                .IsRequired()
                .HasColumnName("StatsColumnCount");

            stats.Property(s => s.IsProcessed)
                .IsRequired()
                .HasColumnName("StatsIsProcessed");

            stats.Property(s => s.UseSeparateTable)
                .IsRequired()
                .HasColumnName("StatsUseSeparateTable");

            stats.Property(s => s.ProcessedAt)
                .HasColumnName("StatsProcessedAt");
        });

        // Indexes
        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("IX_DataSets_UserId");

        builder.HasIndex(d => d.UploadedAt)
            .HasDatabaseName("IX_DataSets_UploadedAt");

        builder.HasIndex(d => new { d.IsDeleted, d.UserId })
            .HasDatabaseName("IX_DataSets_IsDeleted_UserId");

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
        builder.ToTable("DataSetRows", "DataNormalization");

        // Primary key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Id)
            .IsRequired()
            .HasComment("Unique identifier for the row");

        builder.Property(r => r.DataSetId)
            .IsRequired()
            .HasComment("Foreign key to the parent dataset");

        builder.Property(r => r.RowIndex)
            .IsRequired()
            .HasComment("Zero-based index of this row within the dataset");

        builder.Property(r => r.Data)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasComment("JSON representation of the row data");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasComment("When this row was created");

        builder.Property(r => r.UpdatedAt)
            .HasComment("When this row was last updated");

        // Relationships
        builder.HasOne(r => r.DataSet)
            .WithMany()
            .HasForeignKey(r => r.DataSetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.DataSetId)
            .HasDatabaseName("IX_DataSetRows_DataSetId");

        builder.HasIndex(r => new { r.DataSetId, r.RowIndex })
            .IsUnique()
            .HasDatabaseName("IX_DataSetRows_DataSetId_RowIndex");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_DataSetRows_CreatedAt");
    }
}