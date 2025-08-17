using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Mapping;

/// <summary>
/// Manual mapping extensions to replace AutoMapper dependency.
/// Provides type-safe, performant object mapping with full control over the mapping logic.
/// </summary>
public static class ManualMapper
{
    #region DataSet Mappings

    /// <summary>
    /// Maps a DataSet entity to DataSetDto
    /// </summary>
    public static DataSetDto ToDto(this DataSet dataSet)
    {
        if (dataSet == null) return null!;

        return new DataSetDto
        {
            Id = dataSet.Id,
            Name = dataSet.Name,
            Description = dataSet.Description,
            FileName = dataSet.FileName,
            FileType = dataSet.FileType,
            FileSize = dataSet.FileSize,
            UploadedAt = dataSet.UploadedAt,
            IsDeleted = dataSet.IsDeleted,
            UserId = dataSet.UserId,
            RowCount = dataSet.RowCount,
            ColumnCount = dataSet.ColumnCount,
            IsProcessed = dataSet.IsProcessed,
            ProcessedAt = dataSet.ProcessedAt,
            PreviewData = dataSet.PreviewData,
            FilePath = dataSet.FilePath,
            StorageProvider = dataSet.StorageProvider,
            Schema = dataSet.Schema,
            DataHash = dataSet.DataHash,
            UseSeparateTable = dataSet.UseSeparateTable,
            ProcessingErrors = dataSet.ProcessingErrors,
            RetentionExpiryDate = dataSet.RetentionExpiryDate
        };
    }

    /// <summary>
    /// Maps a CreateDataSetDto to DataSet entity
    /// </summary>
    public static DataSet ToEntity(this CreateDataSetDto dto)
    {
        if (dto == null) return null!;

        return new DataSet
        {
            Name = dto.Name,
            Description = dto.Description,
            UserId = dto.UserId,
            UploadedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps a collection of DataSet entities to DataSetDto collection
    /// </summary>
    public static IEnumerable<DataSetDto> ToDtoCollection(this IEnumerable<DataSet> dataSets)
    {
        return dataSets?.Select(ds => ds.ToDto()) ?? Enumerable.Empty<DataSetDto>();
    }

    #endregion

    #region Analysis Mappings

    /// <summary>
    /// Maps an Analysis entity to AnalysisDto
    /// </summary>
    public static AnalysisDto ToDto(this Analysis analysis)
    {
        if (analysis == null) return null!;

        return new AnalysisDto
        {
            Id = analysis.Id,
            Name = analysis.Name,
            Description = analysis.Description,
            Type = analysis.Type,
            CreatedAt = analysis.CreatedAt,
            CompletedAt = analysis.CompletedAt,
            Status = analysis.Status,
            Results = analysis.Results,
            ErrorMessage = analysis.ErrorMessage,
            DataSetId = analysis.DataSetId,
            ComparisonDataSetId = analysis.ComparisonDataSetId
        };
    }

    /// <summary>
    /// Maps a CreateAnalysisDto to Analysis entity
    /// </summary>
    public static Analysis ToEntity(this CreateAnalysisDto dto)
    {
        if (dto == null) return null!;

        return new Analysis
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            DataSetId = dto.DataSetId,
            ComparisonDataSetId = dto.ComparisonDataSetId,
            Configuration = dto.Configuration,
            CreatedAt = DateTime.UtcNow,
            Status = AnalysisStatus.Pending
        };
    }

    /// <summary>
    /// Maps a collection of Analysis entities to AnalysisDto collection
    /// </summary>
    public static IEnumerable<AnalysisDto> ToDtoCollection(this IEnumerable<Analysis> analyses)
    {
        return analyses?.Select(a => a.ToDto()) ?? Enumerable.Empty<AnalysisDto>();
    }

    #endregion

    #region UserSettings Mappings

    /// <summary>
    /// Maps a UserSettings entity to UserSettingsDto
    /// </summary>
    public static UserSettingsDto ToDto(this UserSettings userSettings)
    {
        if (userSettings == null) return null!;

        return new UserSettingsDto
        {
            Id = userSettings.Id,
            UserId = userSettings.UserId,
            Theme = userSettings.Theme,
            Language = userSettings.Language,
            TimeZone = userSettings.TimeZone,
            DateFormat = userSettings.DateFormat,
            TimeFormat = userSettings.TimeFormat,
            EmailNotificationsEnabled = userSettings.EmailNotificationsEnabled,
            PushNotificationsEnabled = userSettings.PushNotificationsEnabled,
            ProcessingCompleteNotifications = userSettings.ProcessingCompleteNotifications,
            ErrorNotifications = userSettings.ErrorNotifications,
            WeeklyDigestEnabled = userSettings.WeeklyDigestEnabled,
            DefaultPageSize = userSettings.DefaultPageSize,
            ShowTutorials = userSettings.ShowTutorials,
            CompactMode = userSettings.CompactMode,
            AutoProcessUploads = userSettings.AutoProcessUploads,
            MaxPreviewRows = userSettings.MaxPreviewRows,
            DefaultFileType = userSettings.DefaultFileType,
            EnableDataValidation = userSettings.EnableDataValidation,
            EnableSchemaInference = userSettings.EnableSchemaInference,
            ShareAnalytics = userSettings.ShareAnalytics,
            AllowDataUsageForImprovement = userSettings.AllowDataUsageForImprovement,
            ShowProcessingTime = userSettings.ShowProcessingTime,
            DisplayName = userSettings.DisplayName,
            CreatedAt = userSettings.CreatedAt,
            UpdatedAt = userSettings.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a UserSettingsDto to UserSettings entity
    /// </summary>
    public static UserSettings ToEntity(this UserSettingsDto dto)
    {
        if (dto == null) return null!;

        return new UserSettings
        {
            Id = dto.Id,
            UserId = dto.UserId,
            Theme = dto.Theme,
            Language = dto.Language,
            TimeZone = dto.TimeZone,
            DateFormat = dto.DateFormat,
            TimeFormat = dto.TimeFormat,
            EmailNotificationsEnabled = dto.EmailNotificationsEnabled,
            PushNotificationsEnabled = dto.PushNotificationsEnabled,
            ProcessingCompleteNotifications = dto.ProcessingCompleteNotifications,
            ErrorNotifications = dto.ErrorNotifications,
            WeeklyDigestEnabled = dto.WeeklyDigestEnabled,
            DefaultPageSize = dto.DefaultPageSize,
            ShowTutorials = dto.ShowTutorials,
            CompactMode = dto.CompactMode,
            AutoProcessUploads = dto.AutoProcessUploads,
            MaxPreviewRows = dto.MaxPreviewRows,
            DefaultFileType = dto.DefaultFileType,
            EnableDataValidation = dto.EnableDataValidation,
            EnableSchemaInference = dto.EnableSchemaInference,
            ShareAnalytics = dto.ShareAnalytics,
            AllowDataUsageForImprovement = dto.AllowDataUsageForImprovement,
            ShowProcessingTime = dto.ShowProcessingTime,
            DisplayName = dto.DisplayName,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// Maps an UpdateUserSettingsDto to UserSettings entity (for updates)
    /// </summary>
    public static UserSettings ToEntity(this UpdateUserSettingsDto dto, UserSettings existingSettings)
    {
        if (dto == null || existingSettings == null) return existingSettings!;

        UpdateStringProperties(dto, existingSettings);
        UpdateBooleanProperties(dto, existingSettings);
        UpdateIntegerProperties(dto, existingSettings);

        existingSettings.UpdatedAt = DateTime.UtcNow;
        return existingSettings;
    }

    private static void UpdateStringProperties(UpdateUserSettingsDto dto, UserSettings existingSettings)
    {
        if (dto.Theme != null) existingSettings.Theme = dto.Theme;
        if (dto.Language != null) existingSettings.Language = dto.Language;
        if (dto.TimeZone != null) existingSettings.TimeZone = dto.TimeZone;
        if (dto.DateFormat != null) existingSettings.DateFormat = dto.DateFormat;
        if (dto.TimeFormat != null) existingSettings.TimeFormat = dto.TimeFormat;
        if (dto.DefaultFileType != null) existingSettings.DefaultFileType = dto.DefaultFileType;
        if (dto.DisplayName != null) existingSettings.DisplayName = dto.DisplayName;
    }

    private static void UpdateBooleanProperties(UpdateUserSettingsDto dto, UserSettings existingSettings)
    {
        if (dto.EmailNotificationsEnabled.HasValue) existingSettings.EmailNotificationsEnabled = dto.EmailNotificationsEnabled.Value;
        if (dto.PushNotificationsEnabled.HasValue) existingSettings.PushNotificationsEnabled = dto.PushNotificationsEnabled.Value;
        if (dto.ProcessingCompleteNotifications.HasValue) existingSettings.ProcessingCompleteNotifications = dto.ProcessingCompleteNotifications.Value;
        if (dto.ErrorNotifications.HasValue) existingSettings.ErrorNotifications = dto.ErrorNotifications.Value;
        if (dto.WeeklyDigestEnabled.HasValue) existingSettings.WeeklyDigestEnabled = dto.WeeklyDigestEnabled.Value;
        if (dto.ShowTutorials.HasValue) existingSettings.ShowTutorials = dto.ShowTutorials.Value;
        if (dto.CompactMode.HasValue) existingSettings.CompactMode = dto.CompactMode.Value;
        if (dto.AutoProcessUploads.HasValue) existingSettings.AutoProcessUploads = dto.AutoProcessUploads.Value;
        if (dto.EnableDataValidation.HasValue) existingSettings.EnableDataValidation = dto.EnableDataValidation.Value;
        if (dto.EnableSchemaInference.HasValue) existingSettings.EnableSchemaInference = dto.EnableSchemaInference.Value;
        if (dto.ShareAnalytics.HasValue) existingSettings.ShareAnalytics = dto.ShareAnalytics.Value;
        if (dto.AllowDataUsageForImprovement.HasValue) existingSettings.AllowDataUsageForImprovement = dto.AllowDataUsageForImprovement.Value;
        if (dto.ShowProcessingTime.HasValue) existingSettings.ShowProcessingTime = dto.ShowProcessingTime.Value;
    }

    private static void UpdateIntegerProperties(UpdateUserSettingsDto dto, UserSettings existingSettings)
    {
        if (dto.DefaultPageSize.HasValue) existingSettings.DefaultPageSize = dto.DefaultPageSize.Value;
        if (dto.MaxPreviewRows.HasValue) existingSettings.MaxPreviewRows = dto.MaxPreviewRows.Value;
    }

    #endregion

    #region UserInfo Mappings

    /// <summary>
    /// Maps Auth0 claims to ProfileInfoDto
    /// </summary>
    public static ProfileInfoDto ToUserInfo(this System.Security.Claims.ClaimsPrincipal user)
    {
        if (user == null) return null!;

        return new ProfileInfoDto
        {
            UserId = user.FindFirst("sub")?.Value ?? user.FindFirst("user_id")?.Value ?? string.Empty,
            Email = user.FindFirst("email")?.Value,
            Name = user.FindFirst("name")?.Value,
            Picture = user.FindFirst("picture")?.Value,
            EmailVerified = bool.TryParse(user.FindFirst("email_verified")?.Value, out var verified) && verified
        };
    }

    #endregion

    #region UserProfile Mappings

    /// <summary>
    /// Maps ProfileInfoDto and UserSettingsDto to UserProfileDto
    /// </summary>
    public static UserProfileDto ToUserProfile(this ProfileInfoDto userInfo, UserSettingsDto userSettings)
    {
        if (userInfo == null) return null!;

        return new UserProfileDto
        {
            UserId = userInfo.UserId,
            Email = userInfo.Email ?? string.Empty,
            Name = userInfo.Name ?? string.Empty,
            Picture = userInfo.Picture ?? string.Empty,
            EmailVerified = userInfo.EmailVerified,
            Settings = userSettings
        };
    }

    #endregion

    #region Generic Collection Mappings

    /// <summary>
    /// Maps a collection of entities to DTOs using a mapping function
    /// </summary>
    public static IEnumerable<TDto> MapCollection<TEntity, TDto>(
        this IEnumerable<TEntity> entities,
        Func<TEntity, TDto> mapper)
    {
        return entities?.Select(mapper) ?? Enumerable.Empty<TDto>();
    }

    #endregion

    #region Null-Safe Mappings

    /// <summary>
    /// Safely maps an entity to DTO, returning null if the entity is null
    /// </summary>
    public static TDto? ToDtoSafe<TEntity, TDto>(this TEntity? entity, Func<TEntity, TDto> mapper)
        where TEntity : class
        where TDto : class
    {
        return entity != null ? mapper(entity) : null;
    }

    /// <summary>
    /// Safely maps a DTO to entity, returning null if the DTO is null
    /// </summary>
    public static TEntity? ToEntitySafe<TDto, TEntity>(this TDto? dto, Func<TDto, TEntity> mapper)
        where TDto : class
        where TEntity : class
    {
        return dto != null ? mapper(dto) : null;
    }

    #endregion
}