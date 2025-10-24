using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for NormalizationAuditLog entity
/// </summary>
public class NormalizationAuditLogConfiguration : IEntityTypeConfiguration<NormalizationAuditLog>
{
    public void Configure(EntityTypeBuilder<NormalizationAuditLog> builder)
    {
        builder.ToTable("normalization_audit_logs", "data_normalization");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");
        
        builder.Property(e => e.NormalizationJobId)
            .HasColumnName("normalization_job_id")
            .IsRequired();
        
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(e => e.Changes)
            .HasColumnName("changes")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();
        
        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);
        
        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        // Indexes for audit queries
        builder.HasIndex(e => e.NormalizationJobId)
            .HasDatabaseName("ix_audit_logs_job_id");
        
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_audit_logs_user_id");
        
        builder.HasIndex(e => e.Action)
            .HasDatabaseName("ix_audit_logs_action");
        
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_audit_logs_timestamp");
        
        builder.HasIndex(e => new { e.NormalizationJobId, e.Timestamp })
            .HasDatabaseName("ix_audit_logs_job_timestamp");
    }
}