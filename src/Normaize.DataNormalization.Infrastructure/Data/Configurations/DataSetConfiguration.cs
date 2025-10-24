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
        builder.ToTable("datasets"); // Map to existing table in main schema
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");
        
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);
        
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(255)
            .IsRequired();

        // Temporarily ignore complex value objects for migration
        builder.Ignore(e => e.FileInfo);
        builder.Ignore(e => e.Statistics);

        builder.Property(e => e.UploadedAt)
            .HasColumnName("uploaded_at")
            .IsRequired();
        
        builder.Property(e => e.Schema)
            .HasColumnName("schema")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.PreviewData)
            .HasColumnName("preview_data")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.ProcessedData)
            .HasColumnName("processed_data")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.ProcessingErrors)
            .HasColumnName("processing_errors")
            .HasColumnType("text");
        
        builder.Property(e => e.RetentionExpiryDate)
            .HasColumnName("retention_expiry_date");

        // Soft delete
        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);
        
        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");
        
        builder.Property(e => e.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(255);

        // Audit trail
        builder.Property(e => e.LastModifiedAt)
            .HasColumnName("last_modified_at")
            .IsRequired();
        
        builder.Property(e => e.LastModifiedBy)
            .HasColumnName("last_modified_by")
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_datasets_user_id");
        
        builder.HasIndex(e => e.UploadedAt)
            .HasDatabaseName("ix_datasets_uploaded_at");
        
        builder.HasIndex(e => new { e.IsDeleted, e.DeletedAt })
            .HasDatabaseName("ix_datasets_soft_delete");
    }
}