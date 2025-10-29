namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing default settings for data processing operations.
/// </summary>
public sealed record ProcessingDefaults
{
    public bool AutoProcessUploads { get; init; }
    public int MaxPreviewRows { get; init; }
    public string DefaultFileType { get; init; }
    public bool EnableDataValidation { get; init; }
    public bool EnableSchemaInference { get; init; }
    public int RetentionDays { get; init; }

    private ProcessingDefaults() // EF Core
    {
        AutoProcessUploads = true;
        MaxPreviewRows = 100;
        DefaultFileType = "CSV";
        EnableDataValidation = true;
        EnableSchemaInference = true;
        RetentionDays = 365;
    }

    private ProcessingDefaults(
        bool autoProcessUploads,
        int maxPreviewRows,
        string defaultFileType,
        bool enableDataValidation,
        bool enableSchemaInference,
        int retentionDays)
    {
        AutoProcessUploads = autoProcessUploads;
        MaxPreviewRows = maxPreviewRows;
        DefaultFileType = defaultFileType;
        EnableDataValidation = enableDataValidation;
        EnableSchemaInference = enableSchemaInference;
        RetentionDays = retentionDays;
    }

    /// <summary>
    /// Creates default processing settings for new users.
    /// </summary>
    public static ProcessingDefaults Default() => new(
        autoProcessUploads: true,
        maxPreviewRows: 100,
        defaultFileType: "CSV",
        enableDataValidation: true,
        enableSchemaInference: true,
        retentionDays: 365
    );

    /// <summary>
    /// Creates processing defaults with validation.
    /// </summary>
    public static ProcessingDefaults Create(
        bool autoProcessUploads,
        int maxPreviewRows,
        string defaultFileType,
        bool enableDataValidation,
        bool enableSchemaInference,
        int retentionDays)
    {
        // Validate preview rows
        if (maxPreviewRows < 10 || maxPreviewRows > 10000)
            throw new ArgumentException("Max preview rows must be between 10 and 10,000", nameof(maxPreviewRows));

        // Validate file type
        var validFileTypes = new[] { "CSV", "JSON", "XML", "EXCEL", "PARQUET", "TXT" };
        if (!validFileTypes.Contains(defaultFileType.ToUpperInvariant()))
            throw new ArgumentException($"Default file type must be one of: {string.Join(", ", validFileTypes)}", nameof(defaultFileType));

        // Validate retention days (1 day to 10 years)
        if (retentionDays < 1 || retentionDays > 3650)
            throw new ArgumentException("Retention days must be between 1 and 3,650 (10 years)", nameof(retentionDays));

        return new ProcessingDefaults(
            autoProcessUploads,
            maxPreviewRows,
            defaultFileType.ToUpperInvariant(),
            enableDataValidation,
            enableSchemaInference,
            retentionDays
        );
    }

    /// <summary>
    /// Creates a copy with updated values.
    /// </summary>
    public ProcessingDefaults With(
        bool? autoProcessUploads = null,
        int? maxPreviewRows = null,
        string? defaultFileType = null,
        bool? enableDataValidation = null,
        bool? enableSchemaInference = null,
        int? retentionDays = null)
    {
        return Create(
            autoProcessUploads ?? AutoProcessUploads,
            maxPreviewRows ?? MaxPreviewRows,
            defaultFileType ?? DefaultFileType,
            enableDataValidation ?? EnableDataValidation,
            enableSchemaInference ?? EnableSchemaInference,
            retentionDays ?? RetentionDays
        );
    }

    /// <summary>
    /// Creates conservative settings (manual processing, validation enabled).
    /// </summary>
    public static ProcessingDefaults Conservative() => new(
        autoProcessUploads: false,
        maxPreviewRows: 50,
        defaultFileType: "CSV",
        enableDataValidation: true,
        enableSchemaInference: true,
        retentionDays: 180
    );

    /// <summary>
    /// Creates aggressive settings (auto-process, higher preview rows, longer retention).
    /// </summary>
    public static ProcessingDefaults Aggressive() => new(
        autoProcessUploads: true,
        maxPreviewRows: 1000,
        defaultFileType: "CSV",
        enableDataValidation: true,
        enableSchemaInference: true,
            retentionDays: 730
        );

}
