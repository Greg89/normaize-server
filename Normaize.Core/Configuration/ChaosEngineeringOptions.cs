namespace Normaize.Core.Configuration;

/// <summary>
/// Configuration options for chaos engineering
/// </summary>
public class ChaosEngineeringOptions
{
    public const string SectionName = "ChaosEngineering";

    /// <summary>
    /// Whether chaos engineering is enabled globally
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Environment where chaos engineering is allowed
    /// </summary>
    public string[] AllowedEnvironments { get; set; } = ["Development", "Staging"];

    /// <summary>
    /// Global probability multiplier for all chaos scenarios (0.0 to 1.0)
    /// </summary>
    public double GlobalProbabilityMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Maximum number of chaos scenarios that can be triggered per minute
    /// </summary>
    public int MaxTriggersPerMinute { get; set; } = 10;

    /// <summary>
    /// Whether to log all chaos engineering activities
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Specific chaos scenario configurations
    /// </summary>
    public Dictionary<string, ChaosScenarioConfig> Scenarios { get; set; } = [];

    /// <summary>
    /// Time-based chaos triggers
    /// </summary>
    public TimeBasedTriggers TimeBasedTriggers { get; set; } = new();

    /// <summary>
    /// User-based chaos triggers
    /// </summary>
    public UserBasedTriggers UserBasedTriggers { get; set; } = new();
}

/// <summary>
/// Configuration for a specific chaos scenario
/// </summary>
public class ChaosScenarioConfig
{
    /// <summary>
    /// Whether this scenario is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Probability of triggering this scenario (0.0 to 1.0)
    /// </summary>
    public double Probability { get; set; } = 0.001;

    /// <summary>
    /// Maximum number of times this scenario can be triggered per hour
    /// </summary>
    public int MaxTriggersPerHour { get; set; } = 5;

    /// <summary>
    /// Whether this scenario should only trigger during specific time windows
    /// </summary>
    public bool TimeWindowRestricted { get; set; } = false;

    /// <summary>
    /// Time windows when this scenario can be triggered (24-hour format)
    /// </summary>
    public List<TimeWindow> AllowedTimeWindows { get; set; } = [];

    /// <summary>
    /// Additional parameters for the scenario
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = [];
}

/// <summary>
/// Time window configuration
/// </summary>
public class TimeWindow
{
    /// <summary>
    /// Start time in 24-hour format (HH:mm)
    /// </summary>
    public string StartTime { get; set; } = "00:00";

    /// <summary>
    /// End time in 24-hour format (HH:mm)
    /// </summary>
    public string EndTime { get; set; } = "23:59";

    /// <summary>
    /// Days of the week when this window applies (0=Sunday, 6=Saturday)
    /// </summary>
    public List<int> DaysOfWeek { get; set; } = [0, 1, 2, 3, 4, 5, 6];
}

/// <summary>
/// Time-based chaos triggers
/// </summary>
public class TimeBasedTriggers
{
    /// <summary>
    /// Whether time-based triggers are enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Chaos scenarios that should be more likely during high-traffic hours
    /// </summary>
    public List<string> HighTrafficScenarios { get; set; } = [];

    /// <summary>
    /// Chaos scenarios that should be more likely during low-traffic hours
    /// </summary>
    public List<string> LowTrafficScenarios { get; set; } = [];

    /// <summary>
    /// High traffic time windows
    /// </summary>
    public List<TimeWindow> HighTrafficWindows { get; set; } =
    [
        new TimeWindow { StartTime = "09:00", EndTime = "17:00", DaysOfWeek = [1, 2, 3, 4, 5] }
    ];
}

/// <summary>
/// User-based chaos triggers
/// </summary>
public class UserBasedTriggers
{
    /// <summary>
    /// Whether user-based triggers are enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// User IDs that should trigger chaos more frequently (for testing)
    /// </summary>
    public List<string> TestUserIds { get; set; } = [];

    /// <summary>
    /// Probability multiplier for test users
    /// </summary>
    public double TestUserProbabilityMultiplier { get; set; } = 10.0;

    /// <summary>
    /// User IDs that should never trigger chaos (excluded users)
    /// </summary>
    public List<string> ExcludedUserIds { get; set; } = [];
}