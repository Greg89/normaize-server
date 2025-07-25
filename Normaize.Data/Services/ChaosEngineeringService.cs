using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;

namespace Normaize.Data.Services;

/// <summary>
/// Centralized chaos engineering service implementation
/// </summary>
public class ChaosEngineeringService : IChaosEngineeringService
{
    private readonly ILogger<ChaosEngineeringService> _logger;
    private readonly ChaosEngineeringOptions _options;
    private readonly IOptionsMonitor<ChaosEngineeringOptions> _optionsMonitor;
    private readonly Random _random;
    private readonly ConcurrentDictionary<string, int> _scenarioCounts = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastTriggerTimes = new();
    private readonly ConcurrentDictionary<string, Func<IDictionary<string, object>?, bool>> _customTriggers = new();
    private readonly ConcurrentDictionary<string, Func<Task>> _customActions = new();
    private readonly ConcurrentQueue<DateTime> _recentTriggers = new();
    private readonly Lock _lockObject = new();

    public ChaosEngineeringService(
        ILogger<ChaosEngineeringService> logger,
        IOptionsMonitor<ChaosEngineeringOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _options = _optionsMonitor.CurrentValue;
        _random = new Random();

        // Subscribe to configuration changes
        _optionsMonitor.OnChange(options =>
        {
            _logger.LogInformation("Chaos engineering configuration updated");
        });

        // Register built-in chaos scenarios
        RegisterBuiltInScenarios();
    }

    public bool ShouldTriggerChaos(string scenarioName, IDictionary<string, object>? context = null)
    {
        return ShouldTriggerChaos(scenarioName, "unknown", "unknown", context);
    }

    public bool ShouldTriggerChaos(string scenarioName, string correlationId, string operationName, IDictionary<string, object>? context = null)
    {
        if (!_options.Enabled)
            return false;

        var currentOptions = _optionsMonitor.CurrentValue;

        if (!IsEnvironmentAllowed(currentOptions))
            return false;

        if (IsRateLimited())
            return false;

        if (ShouldTriggerForUser(context, currentOptions, scenarioName))
            return true;

        if (ShouldTriggerCustomScenario(scenarioName, context))
            return true;

        return ShouldTriggerBuiltInScenario(scenarioName, currentOptions);
    }

    private static bool IsEnvironmentAllowed(ChaosEngineeringOptions currentOptions)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        return currentOptions.AllowedEnvironments.Contains(environment);
    }

    private bool ShouldTriggerForUser(IDictionary<string, object>? context, ChaosEngineeringOptions currentOptions, string scenarioName)
    {
        if (context?.TryGetValue(AppConstants.DataStructures.USER_ID, out var userId) != true || !currentOptions.UserBasedTriggers.Enabled)
            return false;

        var userIdStr = userId?.ToString();
        if (string.IsNullOrEmpty(userIdStr))
            return false;

        if (currentOptions.UserBasedTriggers.ExcludedUserIds.Contains(userIdStr))
            return false;

        if (currentOptions.UserBasedTriggers.TestUserIds.Contains(userIdStr))
        {
            var probability = GetScenarioProbability(scenarioName) *
                            currentOptions.UserBasedTriggers.TestUserProbabilityMultiplier *
                            currentOptions.GlobalProbabilityMultiplier;
            return _random.NextDouble() < probability;
        }

        return false;
    }

    private bool ShouldTriggerCustomScenario(string scenarioName, IDictionary<string, object>? context)
    {
        return _customTriggers.TryGetValue(scenarioName, out var customTrigger) && customTrigger(context);
    }

    private bool ShouldTriggerBuiltInScenario(string scenarioName, ChaosEngineeringOptions currentOptions)
    {
        if (!currentOptions.Scenarios.TryGetValue(scenarioName, out var scenarioConfig))
        {
            // Default low probability for unknown scenarios
            return _random.NextDouble() < (0.001 * currentOptions.GlobalProbabilityMultiplier);
        }

        if (!scenarioConfig.Enabled)
            return false;

        if (scenarioConfig.TimeWindowRestricted && !IsInAllowedTimeWindow(scenarioConfig.AllowedTimeWindows))
            return false;

        if (IsHourlyRateLimited(scenarioName, scenarioConfig.MaxTriggersPerHour))
            return false;

        return _random.NextDouble() < (scenarioConfig.Probability * currentOptions.GlobalProbabilityMultiplier);
    }

    public async Task<bool> ExecuteChaosAsync(string scenarioName, Func<Task> action, IDictionary<string, object>? context = null)
    {
        return await ExecuteChaosAsync(scenarioName, AppConstants.Messages.UNKNOWN, AppConstants.Messages.UNKNOWN, action, context);
    }

    public async Task<bool> ExecuteChaosAsync(string scenarioName, string correlationId, string operationName, Func<Task> action, IDictionary<string, object>? context = null)
    {
        if (!ShouldTriggerChaos(scenarioName, correlationId, operationName, context))
            return false;

        try
        {
            RecordTrigger(scenarioName);

            if (_options.EnableLogging)
            {
                _logger.LogWarning("Chaos engineering triggered: {ScenarioName} for operation {OperationName}. CorrelationId: {CorrelationId}. Context: {@Context}",
                    scenarioName, operationName, correlationId, context);
            }

            // Execute custom action if registered
            if (_customActions.TryGetValue(scenarioName, out var customAction))
            {
                await customAction();
            }
            else
            {
                // Execute provided action
                await action();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chaos engineering scenario failed: {ScenarioName} for operation {OperationName}. CorrelationId: {CorrelationId}",
                scenarioName, operationName, correlationId);
            return false;
        }
    }

    public async Task<T?> ExecuteChaosAsync<T>(string scenarioName, Func<Task<T>> action, IDictionary<string, object>? context = null)
    {
        return await ExecuteChaosAsync(scenarioName, AppConstants.Messages.UNKNOWN, AppConstants.Messages.UNKNOWN, action, context);
    }

    public async Task<T?> ExecuteChaosAsync<T>(string scenarioName, string correlationId, string operationName, Func<Task<T>> action, IDictionary<string, object>? context = null)
    {
        if (!ShouldTriggerChaos(scenarioName, correlationId, operationName, context))
            return default;

        try
        {
            RecordTrigger(scenarioName);

            if (_options.EnableLogging)
            {
                _logger.LogWarning("Chaos engineering triggered: {ScenarioName} for operation {OperationName}. CorrelationId: {CorrelationId}. Context: {@Context}",
                    scenarioName, operationName, correlationId, context);
            }

            // Execute custom action if registered
            if (_customActions.TryGetValue(scenarioName, out var customAction))
            {
                await customAction();
                return default;
            }
            else
            {
                // Execute provided action
                return await action();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chaos engineering scenario failed: {ScenarioName} for operation {OperationName}. CorrelationId: {CorrelationId}",
                scenarioName, operationName, correlationId);
            return default;
        }
    }

    public void RegisterChaosScenario(string scenarioName, Func<IDictionary<string, object>?, bool> triggerCondition, Func<Task> chaosAction)
    {
        ArgumentNullException.ThrowIfNull(triggerCondition);
        ArgumentNullException.ThrowIfNull(chaosAction);
        _customTriggers[scenarioName] = triggerCondition;
        _customActions[scenarioName] = chaosAction;

        _logger.LogInformation("Registered custom chaos scenario: {ScenarioName}", scenarioName);
    }

    public ChaosEngineeringStats GetStats()
    {
        return new ChaosEngineeringStats
        {
            TotalScenarios = _scenarioCounts.Count,
            TriggeredScenarios = _scenarioCounts.Values.Sum(),
            ScenarioCounts = new ConcurrentDictionary<string, int>(_scenarioCounts),
            LastTriggered = _lastTriggerTimes.Values.Count > 0 ? _lastTriggerTimes.Values.Max() : DateTime.MinValue
        };
    }

    private void RegisterBuiltInScenarios()
    {
        // Register common chaos scenarios that can be used across services
        RegisterChaosScenario("ProcessingDelay",
            (context) => _random.NextDouble() < 0.001,
            async () => await Task.Delay(_random.Next(1000, 5000)));

        RegisterChaosScenario("DatabaseTimeout",
            (context) => _random.NextDouble() < 0.0005,
            async () => await Task.Delay(_random.Next(5000, 15000)));

        RegisterChaosScenario("CacheFailure",
            (context) => _random.NextDouble() < 0.0003,
            () => { throw new InvalidOperationException("Simulated cache failure"); });

        RegisterChaosScenario("StorageFailure",
            (context) => _random.NextDouble() < 0.0002,
            () => { throw new InvalidOperationException("Simulated storage failure"); });

        RegisterChaosScenario("NetworkLatency",
            (context) => _random.NextDouble() < 0.002,
            async () => await Task.Delay(_random.Next(500, 2000)));

        RegisterChaosScenario("MemoryPressure",
            (context) => _random.NextDouble() < 0.0001,
            async () =>
            {
                // Simulate memory pressure by allocating temporary objects
                var tempObjects = new List<byte[]>();
                for (int i = 0; i < 100; i++)
                {
                    tempObjects.Add(new byte[1024 * 1024]); // 1MB each
                }
                await Task.Delay(100);
                tempObjects.Clear();
            });
    }

    private double GetScenarioProbability(string scenarioName)
    {
        var currentOptions = _optionsMonitor.CurrentValue;

        if (currentOptions.Scenarios.TryGetValue(scenarioName, out var config))
        {
            return config.Probability;
        }

        return 0.001; // Default probability
    }

    private bool IsRateLimited()
    {
        var currentOptions = _optionsMonitor.CurrentValue;
        var now = DateTime.UtcNow;

        // Remove old entries
        while (_recentTriggers.TryPeek(out var oldest) &&
               now - oldest > TimeSpan.FromMinutes(1))
        {
            _recentTriggers.TryDequeue(out _);
        }

        return _recentTriggers.Count >= currentOptions.MaxTriggersPerMinute;
    }

    private bool IsHourlyRateLimited(string scenarioName, int maxPerHour)
    {
        if (_lastTriggerTimes.TryGetValue(scenarioName, out var lastTrigger))
        {
            if (DateTime.UtcNow - lastTrigger < TimeSpan.FromHours(1))
            {
                var count = _scenarioCounts.GetOrAdd(scenarioName, 0);
                return count >= maxPerHour;
            }
            else
            {
                // Reset count for new hour
                _scenarioCounts.TryRemove(scenarioName, out _);
            }
        }

        return false;
    }

    private static bool IsInAllowedTimeWindow(List<TimeWindow> allowedWindows)
    {
        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;
        var currentDayOfWeek = (int)now.DayOfWeek;

        return allowedWindows.Any(window =>
        {
            if (!window.DaysOfWeek.Contains(currentDayOfWeek))
                return false;

            var startTime = TimeSpan.Parse(window.StartTime, CultureInfo.InvariantCulture);
            var endTime = TimeSpan.Parse(window.EndTime, CultureInfo.InvariantCulture);

            if (startTime <= endTime)
            {
                return currentTime >= startTime && currentTime <= endTime;
            }
            else
            {
                // Handle overnight windows (e.g., 22:00 to 06:00)
                return currentTime >= startTime || currentTime <= endTime;
            }
        });
    }

    private void RecordTrigger(string scenarioName)
    {
        var now = DateTime.UtcNow;

        _scenarioCounts.AddOrUpdate(scenarioName, 1, (key, oldValue) => oldValue + 1);
        _lastTriggerTimes[scenarioName] = now;

        lock (_lockObject)
        {
            _recentTriggers.Enqueue(now);
        }
    }
}