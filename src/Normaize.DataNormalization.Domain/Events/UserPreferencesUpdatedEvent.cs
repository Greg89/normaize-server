namespace Normaize.DataNormalization.Domain.Events;

/// <summary>
/// Domain event raised when user preferences are updated.
/// </summary>
public sealed class UserPreferencesUpdatedEvent
{
    public Guid UserId { get; }
    public string Auth0UserId { get; }
    public DateTime UpdatedAt { get; }
    public string? UpdatedBy { get; }

    public UserPreferencesUpdatedEvent(Guid userId, string auth0UserId, DateTime updatedAt, string? updatedBy)
    {
        UserId = userId;
        Auth0UserId = auth0UserId;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }
}
