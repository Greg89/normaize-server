namespace Normaize.Core.Interfaces;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckLivenessAsync(CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckReadinessAsync(CancellationToken cancellationToken = default);
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class ComponentHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public TimeSpan Duration { get; set; }
} 