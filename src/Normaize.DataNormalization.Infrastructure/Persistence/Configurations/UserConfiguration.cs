using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for User aggregate
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Auth0UserId - unique index for fast lookups
        builder.Property(u => u.Auth0UserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Auth0UserId)
            .IsUnique()
            .HasDatabaseName("IX_Users_Auth0UserId");

        // Basic properties
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(u => u.IsDeleted)
            .HasDatabaseName("IX_Users_IsDeleted");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        // Configure UserPreferences as owned entity
        builder.OwnsOne(u => u.Preferences, prefs =>
        {
            prefs.Property(p => p.Theme)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Preferences_Theme");

            prefs.Property(p => p.Language)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("Preferences_Language");

            prefs.Property(p => p.TimeZone)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Preferences_TimeZone");

            prefs.Property(p => p.DateFormat)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Preferences_DateFormat");

            prefs.Property(p => p.TimeFormat)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("Preferences_TimeFormat");

            prefs.Property(p => p.DefaultPageSize)
                .IsRequired()
                .HasColumnName("Preferences_DefaultPageSize");

            prefs.Property(p => p.ShowTutorials)
                .IsRequired()
                .HasColumnName("Preferences_ShowTutorials");

            prefs.Property(p => p.CompactMode)
                .IsRequired()
                .HasColumnName("Preferences_CompactMode");
        });

        // Configure NotificationSettings as owned entity
        builder.OwnsOne(u => u.NotificationSettings, notifs =>
        {
            notifs.Property(n => n.EmailNotificationsEnabled)
                .IsRequired()
                .HasColumnName("NotificationSettings_EmailNotificationsEnabled");

            notifs.Property(n => n.PushNotificationsEnabled)
                .IsRequired()
                .HasColumnName("NotificationSettings_PushNotificationsEnabled");

            notifs.Property(n => n.ProcessingCompleteNotifications)
                .IsRequired()
                .HasColumnName("NotificationSettings_ProcessingCompleteNotifications");

            notifs.Property(n => n.ErrorNotifications)
                .IsRequired()
                .HasColumnName("NotificationSettings_ErrorNotifications");

            notifs.Property(n => n.WeeklyDigestEnabled)
                .IsRequired()
                .HasColumnName("NotificationSettings_WeeklyDigestEnabled");
        });

        // Configure ProcessingDefaults as owned entity
        builder.OwnsOne(u => u.ProcessingDefaults, processing =>
        {
            processing.Property(p => p.AutoProcessUploads)
                .IsRequired()
                .HasColumnName("ProcessingDefaults_AutoProcessUploads");

            processing.Property(p => p.MaxPreviewRows)
                .IsRequired()
                .HasColumnName("ProcessingDefaults_MaxPreviewRows");

            processing.Property(p => p.DefaultFileType)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("ProcessingDefaults_DefaultFileType");

            processing.Property(p => p.EnableDataValidation)
                .IsRequired()
                .HasColumnName("ProcessingDefaults_EnableDataValidation");

            processing.Property(p => p.EnableSchemaInference)
                .IsRequired()
                .HasColumnName("ProcessingDefaults_EnableSchemaInference");

            processing.Property(p => p.RetentionDays)
                .IsRequired()
                .HasColumnName("ProcessingDefaults_RetentionDays");
        });

        // Configure PrivacySettings as owned entity
        builder.OwnsOne(u => u.PrivacySettings, privacy =>
        {
            privacy.Property(p => p.ShareAnalytics)
                .IsRequired()
                .HasColumnName("PrivacySettings_ShareAnalytics");

            privacy.Property(p => p.AllowDataUsageForImprovement)
                .IsRequired()
                .HasColumnName("PrivacySettings_AllowDataUsageForImprovement");

            privacy.Property(p => p.ShowProcessingTime)
                .IsRequired()
                .HasColumnName("PrivacySettings_ShowProcessingTime");
        });

        // Ignore domain events (they're not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}
