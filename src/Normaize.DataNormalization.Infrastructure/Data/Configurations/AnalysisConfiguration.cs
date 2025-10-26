using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Analysis aggregate
/// </summary>
public class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
    public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.ToTable("analyses", "data_normalization");

        builder.HasKey(e => e.Id);

        // Configure AnalysisId value object
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(
                v => v.Value,
                v => new AnalysisId(v))
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.DataSetId)
            .HasColumnName("dataset_id")
            .IsRequired();

        builder.Property(e => e.ComparisonDataSetId)
            .HasColumnName("comparison_dataset_id");

        // Configure AnalysisConfiguration value object
        builder.Property(e => e.Configuration)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? v.JsonConfiguration : null,
                v => v != null ? new Domain.ValueObjects.AnalysisConfiguration(v) : null);

        // Configure AnalysisResult value object
        builder.Property(e => e.Result)
            .HasColumnName("result")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? v.JsonResult : null,
                v => v != null ? new Domain.ValueObjects.AnalysisResult(v) : null);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        // Soft delete properties
        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(e => e.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        // Navigation properties
        builder.HasOne(e => e.DataSet)
            .WithMany()
            .HasForeignKey(e => e.DataSetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ComparisonDataSet)
            .WithMany()
            .HasForeignKey(e => e.ComparisonDataSetId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_analyses_status");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("ix_analyses_type");

        builder.HasIndex(e => e.DataSetId)
            .HasDatabaseName("ix_analyses_dataset_id");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_analyses_created_at");

        builder.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("ix_analyses_is_deleted");

        builder.HasIndex(e => new { e.Status, e.Type })
            .HasDatabaseName("ix_analyses_status_type");

        builder.HasIndex(e => new { e.DataSetId, e.Status })
            .HasDatabaseName("ix_analyses_dataset_status");

        // Global query filter to exclude soft-deleted records
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Ignore domain events (these are handled by the application layer)
        builder.Ignore(e => e.DomainEvents);
    }
}