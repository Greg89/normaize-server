namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing user privacy preferences.
/// </summary>
public sealed record PrivacySettings
{
    public bool ShareAnalytics { get; init; }
    public bool AllowDataUsageForImprovement { get; init; }
    public bool ShowProcessingTime { get; init; }

    private PrivacySettings() // EF Core
    {
        ShareAnalytics = true;
        AllowDataUsageForImprovement = false;
        ShowProcessingTime = true;
    }

    private PrivacySettings(
        bool shareAnalytics,
        bool allowDataUsageForImprovement,
        bool showProcessingTime)
    {
        ShareAnalytics = shareAnalytics;
        AllowDataUsageForImprovement = allowDataUsageForImprovement;
        ShowProcessingTime = showProcessingTime;
    }

    /// <summary>
    /// Creates default privacy settings for new users.
    /// </summary>
    public static PrivacySettings Default() => new(
        shareAnalytics: true,
        allowDataUsageForImprovement: false,
        showProcessingTime: true
    );

    /// <summary>
    /// Creates privacy settings with specified values.
    /// </summary>
    public static PrivacySettings Create(
        bool shareAnalytics,
        bool allowDataUsageForImprovement,
        bool showProcessingTime)
    {
        return new PrivacySettings(
            shareAnalytics,
            allowDataUsageForImprovement,
            showProcessingTime
        );
    }

    /// <summary>
    /// Creates a copy with updated values.
    /// </summary>
    public PrivacySettings With(
        bool? shareAnalytics = null,
        bool? allowDataUsageForImprovement = null,
        bool? showProcessingTime = null)
    {
        return Create(
            shareAnalytics ?? ShareAnalytics,
            allowDataUsageForImprovement ?? AllowDataUsageForImprovement,
            showProcessingTime ?? ShowProcessingTime
        );
    }

    /// <summary>
    /// Creates most private settings (everything disabled).
    /// </summary>
    public static PrivacySettings MostPrivate() => Create(
        shareAnalytics: false,
        allowDataUsageForImprovement: false,
        showProcessingTime: false
    );

    /// <summary>
    /// Creates most open settings (everything enabled).
    /// </summary>
    public static PrivacySettings MostOpen() => Create(
        shareAnalytics: true,
        allowDataUsageForImprovement: true,
        showProcessingTime: true
    );
}
