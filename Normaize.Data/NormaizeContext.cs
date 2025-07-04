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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DataSet configuration
        modelBuilder.Entity<DataSet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FileName).IsRequired();
            entity.Property(e => e.FileType).IsRequired();
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.IsProcessed).HasDefaultValue(false);
        });

        // Analysis configuration
        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.DataSet)
                  .WithMany(d => d.Analyses)
                  .HasForeignKey(e => e.DataSetId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.ComparisonDataSet)
                  .WithMany()
                  .HasForeignKey(e => e.ComparisonDataSetId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
} 