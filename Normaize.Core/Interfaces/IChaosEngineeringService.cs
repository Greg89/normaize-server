using System.Collections.Concurrent;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Centralized chaos engineering service for injecting controlled failures
/// and testing system resilience across all services.
/// </summary>
public interface IChaosEngineeringService
{
    /// <summary>
    /// Determines if a chaos scenario should be triggered based on configuration
    /// </summary>
    /// <param name="scenarioName">Name of the chaos scenario</param>
    /// <param name="context">Additional context for the scenario</param>
    /// <returns>True if the scenario should be triggered</returns>
    bool ShouldTriggerChaos(string scenarioName, IDictionary<string, object>? context = null);

    /// <summary>
    /// Determines if a chaos scenario should be triggered with correlation info
    /// </summary>
    /// <param name="scenarioName">Name of the chaos scenario</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="operationName">Name of the operation that triggered chaos</param>
    /// <param name="context">Additional context for the scenario</param>
    /// <returns>True if the scenario should be triggered</returns>
    bool ShouldTriggerChaos(string scenarioName, string correlationId, string operationName, IDictionary<string, object>? context = null);

    /// <summary>
    /// Executes a chaos scenario if conditions are met
    /// </summary>
    /// <param name="scenarioName">Name of the chaos scenario</param>
    /// <param name="action">Action to execute if chaos is triggered</param>
    /// <param name="context">Additional context for the scenario</param>
    /// <returns>True if chaos was triggered and executed</returns>
    Task<bool> ExecuteChaosAsync(string scenarioName, Func<Task> action, IDictionary<string, object>? context = null);

    /// <summary>
    /// Executes a chaos scenario with correlation info
    /// </summary>
    /// <param name="scenarioName">Name of the chaos scenario</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="operationName">Name of the operation that triggered chaos</param>
    /// <param name="action">Action to execute if chaos is triggered</param>
    /// <param name="context">Additional context for the scenario</param>
    /// <returns>True if chaos was triggered and executed</returns>
    Task<bool> ExecuteChaosAsync(string scenarioName, string correlationId, string operationName, Func<Task> action, IDictionary<string, object>? context = null);

    /// <summary>
    /// Executes a chaos scenario with a return value
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="scenarioName">Name of the chaos scenario</param>
    /// <param name="action">Action to execute if chaos is triggered</param>
    /// <param name="context">Additional context for the scenario</param>
    /// <returns>Result of the chaos action or default value</returns>
    Task<T?> ExecuteChaosAsync<T>(string scenarioName, Func<Task<T>> action, IDictionary<string, object>? context = null);

    /// <summary>
    /// Executes a chaos scenario with a return value and correlation info
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="scenarioName">Name of the chaos scenario</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="operationName">Name of the operation that triggered chaos</param>
    /// <param name="action">Action to execute if chaos is triggered</param>
    /// <param name="context">Additional context for the scenario</param>
    /// <returns>Result of the chaos action or default value</returns>
    Task<T?> ExecuteChaosAsync<T>(string scenarioName, string correlationId, string operationName, Func<Task<T>> action, IDictionary<string, object>? context = null);

    /// <summary>
    /// Registers a custom chaos scenario
    /// </summary>
    /// <param name="scenarioName">Name of the scenario</param>
    /// <param name="triggerCondition">Function that determines when to trigger</param>
    /// <param name="chaosAction">Action to execute when triggered</param>
    void RegisterChaosScenario(string scenarioName, Func<IDictionary<string, object>?, bool> triggerCondition, Func<Task> chaosAction);

    /// <summary>
    /// Gets current chaos engineering statistics
    /// </summary>
    /// <returns>Statistics about chaos scenarios</returns>
    ChaosEngineeringStats GetStats();
}

/// <summary>
/// Statistics for chaos engineering scenarios
/// </summary>
public class ChaosEngineeringStats
{
    public int TotalScenarios { get; set; }
    public int TriggeredScenarios { get; set; }
    public ConcurrentDictionary<string, int> ScenarioCounts { get; set; } = new();
    public DateTime LastTriggered { get; set; }
}