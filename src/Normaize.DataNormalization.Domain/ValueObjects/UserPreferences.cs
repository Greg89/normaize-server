namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing user interface and experience preferences.
/// </summary>
public sealed record UserPreferences
{
    public string Theme { get; init; }
    public string Language { get; init; }
    public string TimeZone { get; init; }
    public string DateFormat { get; init; }
    public string TimeFormat { get; init; }
    public int DefaultPageSize { get; init; }
    public bool ShowTutorials { get; init; }
    public bool CompactMode { get; init; }

    private UserPreferences() // EF Core
    {
        Theme = "light";
        Language = "en";
        TimeZone = "UTC";
        DateFormat = "MM/dd/yyyy";
        TimeFormat = "12h";
        DefaultPageSize = 20;
        ShowTutorials = true;
        CompactMode = false;
    }

    private UserPreferences(
        string theme,
        string language,
        string timeZone,
        string dateFormat,
        string timeFormat,
        int defaultPageSize,
        bool showTutorials,
        bool compactMode)
    {
        Theme = theme;
        Language = language;
        TimeZone = timeZone;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        DefaultPageSize = defaultPageSize;
        ShowTutorials = showTutorials;
        CompactMode = compactMode;
    }

    /// <summary>
    /// Creates default user preferences for new users.
    /// </summary>
    public static UserPreferences Default() => new(
        theme: "light",
        language: "en",
        timeZone: "UTC",
        dateFormat: "MM/dd/yyyy",
        timeFormat: "12h",
        defaultPageSize: 20,
        showTutorials: true,
        compactMode: false
    );

    /// <summary>
    /// Creates user preferences with validation.
    /// </summary>
    public static UserPreferences Create(
        string theme,
        string language,
        string timeZone,
        string dateFormat,
        string timeFormat,
        int defaultPageSize,
        bool showTutorials,
        bool compactMode)
    {
        // Validate theme
        var validThemes = new[] { "light", "dark", "auto" };
        if (!validThemes.Contains(theme.ToLowerInvariant()))
            throw new ArgumentException($"Theme must be one of: {string.Join(", ", validThemes)}", nameof(theme));

        // Validate language (ISO 639-1 codes)
        if (string.IsNullOrWhiteSpace(language) || language.Length != 2)
            throw new ArgumentException("Language must be a valid ISO 639-1 code (e.g., 'en', 'es', 'fr')", nameof(language));

        // Validate page size
        if (defaultPageSize < 10 || defaultPageSize > 100)
            throw new ArgumentException("Default page size must be between 10 and 100", nameof(defaultPageSize));

        // Validate time format
        var validTimeFormats = new[] { "12h", "24h" };
        if (!validTimeFormats.Contains(timeFormat.ToLowerInvariant()))
            throw new ArgumentException($"Time format must be one of: {string.Join(", ", validTimeFormats)}", nameof(timeFormat));

        // Note: TimeZone, DateFormat validation could be more strict, but keeping flexible for now

        return new UserPreferences(
            theme.ToLowerInvariant(),
            language.ToLowerInvariant(),
            timeZone,
            dateFormat,
            timeFormat.ToLowerInvariant(),
            defaultPageSize,
            showTutorials,
            compactMode
        );
    }

    /// <summary>
    /// Creates a copy with updated values.
    /// </summary>
    public UserPreferences With(
        string? theme = null,
        string? language = null,
        string? timeZone = null,
        string? dateFormat = null,
        string? timeFormat = null,
        int? defaultPageSize = null,
        bool? showTutorials = null,
        bool? compactMode = null)
    {
        return Create(
            theme ?? Theme,
            language ?? Language,
            timeZone ?? TimeZone,
            dateFormat ?? DateFormat,
            timeFormat ?? TimeFormat,
            defaultPageSize ?? DefaultPageSize,
            showTutorials ?? ShowTutorials,
            compactMode ?? CompactMode
        );
    }
}
