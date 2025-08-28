namespace Normaize.Core.Constants;

/// <summary>
/// Event-driven architecture related constants
/// </summary>
public static class EventDrivenConstants
{
    /// <summary>
    /// Event-driven architecture constants
    /// </summary>
    public static class EventDriven
    {
        // Event bus types
        public const string IN_MEMORY = "InMemory";
        public const string REDIS = "Redis";
        public const string DATABASE = "Database";
        public const string SIGNALR = "SignalR";

        // Event persistence
        public const string EVENT_STORE = "EventStore";
        public const string OUTBOX_PATTERN = "OutboxPattern";

        // Retry policies
        public const int MAX_EVENT_RETRY_ATTEMPTS = 3;
        public const int EVENT_RETRY_DELAY_MS = 1000;
        public const int EVENT_PROCESSING_TIMEOUT_MS = 30000;
    }
}
