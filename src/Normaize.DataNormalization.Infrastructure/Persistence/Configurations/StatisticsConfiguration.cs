using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;
using System.Text.Json;

namespace Normaize.DataNormalization.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Statistics aggregate
/// </summary>
public class StatisticsConfiguration : IEntityTypeConfiguration<Statistics>
{
    public void Configure(EntityTypeBuilder<Statistics> builder)
    {
        builder.ToTable("Statistics");

        // Primary key
        builder.HasKey(s => s.Id);

        // Configure StatisticsId value object
        builder.Property(s => s.Id)
            .HasConversion(
                statisticsId => statisticsId.Value,
                value => new StatisticsId(value))
            .ValueGeneratedOnAdd();

        // Basic properties
        builder.Property(s => s.DataSetId)
            .IsRequired();

        builder.Property(s => s.DataSetName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.TotalRows)
            .IsRequired();

        builder.Property(s => s.TotalColumns)
            .IsRequired();

        builder.Property(s => s.MissingValues)
            .IsRequired();

        builder.Property(s => s.DuplicateRows)
            .IsRequired();

        builder.Property(s => s.CalculatedAt)
            .IsRequired();

        builder.Property(s => s.ProcessingTime)
            .IsRequired()
            .HasConversion(
                timeSpan => timeSpan.TotalMilliseconds,
                milliseconds => TimeSpan.FromMilliseconds(milliseconds));

        builder.Property(s => s.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Complex properties stored as JSON
        builder.Property(s => s.ColumnSummaries)
            .HasConversion(
                summaries => JsonSerializer.Serialize(summaries, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, ColumnSummary>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, ColumnSummary>())
            .HasColumnType("jsonb");

        builder.Property(s => s.ColumnStatistics)
            .HasConversion(
                statistics => JsonSerializer.Serialize(statistics, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, StatisticalMeasure>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, StatisticalMeasure>())
            .HasColumnType("jsonb");

        // Indexes for performance
        builder.HasIndex(s => s.DataSetId)
            .HasDatabaseName("IX_Statistics_DataSetId");

        builder.HasIndex(s => s.CalculatedAt)
            .HasDatabaseName("IX_Statistics_CalculatedAt");

        builder.HasIndex(s => s.IsDeleted)
            .HasDatabaseName("IX_Statistics_IsDeleted");

        // Ignore domain events (they're not persisted)
        builder.Ignore(s => s.DomainEvents);
    }
}