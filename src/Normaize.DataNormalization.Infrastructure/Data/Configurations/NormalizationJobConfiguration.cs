using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for NormalizationJob aggregate
/// </summary>
public class NormalizationJobConfiguration : IEntityTypeConfiguration<NormalizationJob>
{
    public void Configure(EntityTypeBuilder<NormalizationJob> builder)
    {
        builder.ToTable("normalization_jobs", "data_normalization");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.DataSetId)
            .HasColumnName("dataset_id")
            .IsRequired();

        builder.Property(e => e.OperationType)
            .HasColumnName("operation_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.OperationParameters)
            .HasColumnName("operation_parameters")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(e => e.MaxRetries)
            .HasColumnName("max_retries")
            .HasDefaultValue(5);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(1000);

        builder.Property(e => e.Result)
            .HasColumnName("result")
            .HasColumnType("jsonb");

        builder.Property(e => e.ProgressPercentage)
            .HasColumnName("progress_percentage")
            .HasDefaultValue(0);

        builder.Property(e => e.ProgressMessage)
            .HasColumnName("progress_message")
            .HasMaxLength(500);

        // Navigation properties
        builder.HasOne(e => e.DataSet)
            .WithMany()
            .HasForeignKey(e => e.DataSetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.AuditLogs)
            .WithOne()
            .HasForeignKey(a => a.NormalizationJobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_normalization_jobs_status");

        builder.HasIndex(e => e.DataSetId)
            .HasDatabaseName("ix_normalization_jobs_dataset_id");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_normalization_jobs_created_at");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("ix_normalization_jobs_status_created_at");
    }
}