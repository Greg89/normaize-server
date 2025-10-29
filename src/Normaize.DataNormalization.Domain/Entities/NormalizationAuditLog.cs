using System;

namespace Normaize.DataNormalization.Domain.Entities;

/// <summary>
/// Audit log entity for tracking domain events and user actions
/// </summary>
public class NormalizationAuditLog
{
    public Guid Id { get; private set; }
    public Guid NormalizationJobId { get; private set; }
    public string UserId { get; private set; }
    public string Action { get; private set; }
    public string? Changes { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private NormalizationAuditLog() // EF Core constructor
    {
        UserId = string.Empty;
        Action = string.Empty;
    }

    private NormalizationAuditLog(
        Guid normalizationJobId,
        string userId,
        string action,
        string? changes = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        Id = Guid.NewGuid();
        NormalizationJobId = normalizationJobId;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Changes = changes;
        Timestamp = DateTime.UtcNow;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// Creates an audit log entry for a domain event
    /// </summary>
    public static NormalizationAuditLog FromDomainEvent(
        Guid jobId,
        string userId,
        string eventType,
        object? eventData = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var changes = eventData != null
            ? System.Text.Json.JsonSerializer.Serialize(eventData)
            : null;

        return new NormalizationAuditLog(jobId, userId, eventType, changes, ipAddress, userAgent);
    }

    /// <summary>
    /// Creates an audit log entry for a user action
    /// </summary>
    public static NormalizationAuditLog ForUserAction(
        Guid jobId,
        string userId,
        string action,
        Dictionary<string, object>? changes = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var changesJson = changes != null
            ? System.Text.Json.JsonSerializer.Serialize(changes)
            : null;

        return new NormalizationAuditLog(jobId, userId, action, changesJson, ipAddress, userAgent);
    }

    // Common audit actions
    public static class Actions
    {
        public const string JobCreated = "JobCreated";
        public const string JobStarted = "JobStarted";
        public const string JobProgressUpdated = "JobProgressUpdated";
        public const string JobCompleted = "JobCompleted";
        public const string JobFailed = "JobFailed";
        public const string JobRetried = "JobRetried";
        public const string JobCancelled = "JobCancelled";
        public const string JobMovedToDeadLetter = "JobMovedToDeadLetter";
        public const string JobDeleted = "JobDeleted";
        public const string JobRestored = "JobRestored";
        public const string ParametersUpdated = "ParametersUpdated";
        public const string PriorityChanged = "PriorityChanged";
    }

    // Helper properties
    public bool HasChanges => !string.IsNullOrWhiteSpace(Changes);
    public bool IsSystemAction => string.IsNullOrWhiteSpace(IpAddress);
    public TimeSpan Age => DateTime.UtcNow - Timestamp;

    /// <summary>
    /// Deserializes the changes JSON to a dictionary
    /// </summary>
    public Dictionary<string, object>? GetChanges()
    {
        if (string.IsNullOrWhiteSpace(Changes))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Changes);
        }
        catch
        {
            return null;
        }
    }
}