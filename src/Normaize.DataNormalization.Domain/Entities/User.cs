using Normaize.DataNormalization.Domain.Events;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Domain.Entities;

/// <summary>
/// User aggregate root representing a user in the system.
/// Maps to external Auth0 identity while maintaining internal consistency.
/// </summary>
public class User
{
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; private set; }
    public string Auth0UserId { get; private set; }
    public string? DisplayName { get; private set; }

    // Value Objects
    public UserPreferences Preferences { get; private set; }
    public NotificationSettings NotificationSettings { get; private set; }
    public ProcessingDefaults ProcessingDefaults { get; private set; }
    public PrivacySettings PrivacySettings { get; private set; }

    // Audit trail
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    private User() // EF Core constructor
    {
        Auth0UserId = string.Empty;
        Preferences = null!;
        NotificationSettings = null!;
        ProcessingDefaults = null!;
        PrivacySettings = null!;
    }

    private User(
        string auth0UserId,
        string? displayName,
        UserPreferences preferences,
        NotificationSettings notificationSettings,
        ProcessingDefaults processingDefaults,
        PrivacySettings privacySettings)
    {
        Id = Guid.NewGuid();
        Auth0UserId = auth0UserId ?? throw new ArgumentNullException(nameof(auth0UserId));
        DisplayName = displayName;
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        NotificationSettings = notificationSettings ?? throw new ArgumentNullException(nameof(notificationSettings));
        ProcessingDefaults = processingDefaults ?? throw new ArgumentNullException(nameof(processingDefaults));
        PrivacySettings = privacySettings ?? throw new ArgumentNullException(nameof(privacySettings));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;

        AddDomainEvent(new UserRegisteredEvent(Id, Auth0UserId, DisplayName, CreatedAt));
    }

    /// <summary>
    /// Registers a new user with default settings.
    /// </summary>
    public static User Register(string auth0UserId, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ArgumentException("Auth0 user ID cannot be null or empty", nameof(auth0UserId));

        return new User(
            auth0UserId,
            displayName,
            UserPreferences.Default(),
            NotificationSettings.Default(),
            ProcessingDefaults.Default(),
            PrivacySettings.Default()
        );
    }

    /// <summary>
    /// Registers a new user with custom settings.
    /// </summary>
    public static User RegisterWithSettings(
        string auth0UserId,
        string? displayName,
        UserPreferences preferences,
        NotificationSettings notificationSettings,
        ProcessingDefaults processingDefaults,
        PrivacySettings privacySettings)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ArgumentException("Auth0 user ID cannot be null or empty", nameof(auth0UserId));

        return new User(
            auth0UserId,
            displayName,
            preferences,
            notificationSettings,
            processingDefaults,
            privacySettings
        );
    }

    /// <summary>
    /// Updates the display name.
    /// </summary>
    public void UpdateDisplayName(string? displayName, string? updatedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted user");

        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPreferencesUpdatedEvent(Id, Auth0UserId, UpdatedAt, updatedBy));
    }

    /// <summary>
    /// Updates user interface preferences.
    /// </summary>
    public void UpdatePreferences(UserPreferences preferences, string? updatedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted user");

        ArgumentNullException.ThrowIfNull(preferences);

        Preferences = preferences;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPreferencesUpdatedEvent(Id, Auth0UserId, UpdatedAt, updatedBy));
    }

    /// <summary>
    /// Updates notification settings.
    /// </summary>
    public void UpdateNotificationSettings(NotificationSettings notificationSettings, string? updatedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted user");

        ArgumentNullException.ThrowIfNull(notificationSettings);

        NotificationSettings = notificationSettings;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPreferencesUpdatedEvent(Id, Auth0UserId, UpdatedAt, updatedBy));
    }

    /// <summary>
    /// Updates data processing defaults.
    /// </summary>
    public void UpdateProcessingDefaults(ProcessingDefaults processingDefaults, string? updatedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted user");

        ArgumentNullException.ThrowIfNull(processingDefaults);

        ProcessingDefaults = processingDefaults;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPreferencesUpdatedEvent(Id, Auth0UserId, UpdatedAt, updatedBy));
    }

    /// <summary>
    /// Updates privacy settings.
    /// </summary>
    public void UpdatePrivacySettings(PrivacySettings privacySettings, string? updatedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted user");

        ArgumentNullException.ThrowIfNull(privacySettings);

        PrivacySettings = privacySettings;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPreferencesUpdatedEvent(Id, Auth0UserId, UpdatedAt, updatedBy));
    }

    /// <summary>
    /// Updates all settings at once.
    /// </summary>
    public void UpdateAllSettings(
        string? displayName,
        UserPreferences preferences,
        NotificationSettings notificationSettings,
        ProcessingDefaults processingDefaults,
        PrivacySettings privacySettings,
        string? updatedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted user");

        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(notificationSettings);
        ArgumentNullException.ThrowIfNull(processingDefaults);
        ArgumentNullException.ThrowIfNull(privacySettings);

        DisplayName = displayName;
        Preferences = preferences;
        NotificationSettings = notificationSettings;
        ProcessingDefaults = processingDefaults;
        PrivacySettings = privacySettings;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPreferencesUpdatedEvent(Id, Auth0UserId, UpdatedAt, updatedBy));
    }

    /// <summary>
    /// Resets all settings to defaults.
    /// </summary>
    public void ResetToDefaults(string? updatedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted user");

        Preferences = UserPreferences.Default();
        NotificationSettings = NotificationSettings.Default();
        ProcessingDefaults = ProcessingDefaults.Default();
        PrivacySettings = PrivacySettings.Default();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPreferencesUpdatedEvent(Id, Auth0UserId, UpdatedAt, updatedBy));
    }

    /// <summary>
    /// Soft deletes the user.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a soft-deleted user.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ensures the user accessing is the owner (by Auth0 ID).
    /// </summary>
    public void EnsureUserAccess(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ArgumentException("Auth0 user ID cannot be null or empty", nameof(auth0UserId));

        if (!Auth0UserId.Equals(auth0UserId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"User {auth0UserId} is not authorized to access this user profile");
    }

    private void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
