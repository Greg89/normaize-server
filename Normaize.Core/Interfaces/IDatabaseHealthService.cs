namespace Normaize.Core.Interfaces;

public interface IDatabaseHealthService
{
    Task<DatabaseHealthResult> CheckHealthAsync();
}

public class DatabaseHealthResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> MissingColumns { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 