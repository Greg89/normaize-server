namespace Normaize.DataNormalization.Domain.Events;

/// <summary>
/// Domain event raised when a new user is registered in the system.
/// </summary>
public sealed class UserRegisteredEvent
{
    public Guid UserId { get; }
    public string Auth0UserId { get; }
    public string? DisplayName { get; }
    public DateTime RegisteredAt { get; }

    public UserRegisteredEvent(Guid userId, string auth0UserId, string? displayName, DateTime registeredAt)
    {
        UserId = userId;
        Auth0UserId = auth0UserId;
        DisplayName = displayName;
        RegisteredAt = registeredAt;
    }
}
