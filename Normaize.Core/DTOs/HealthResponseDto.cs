using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Data Transfer Object for basic health check responses
/// </summary>
/// <remarks>
/// This DTO provides essential health status information for basic health check endpoints.
/// It includes the current health status, timestamp, service identification, version information,
/// and environment details. This is used by the HealthController for simple health monitoring
/// and status reporting.
/// 
/// Unlike more detailed health monitoring DTOs, this provides a lightweight response
/// suitable for basic health checks and load balancer health probes.
/// </remarks>
public class HealthResponseDto
{
    /// <summary>
    /// Gets or sets the current health status of the service
    /// </summary>
    /// <remarks>
    /// Indicates the overall health status of the application (e.g., "healthy", "unhealthy", "degraded").
    /// This is typically used by load balancers and monitoring systems to determine service availability.
    /// </remarks>
    [Required]
    [StringLength(50)]
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the health check was performed
    /// </summary>
    /// <remarks>
    /// The UTC timestamp when this health check response was generated.
    /// This helps with monitoring and debugging by providing temporal context.
    /// </remarks>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the name of the service being health checked
    /// </summary>
    /// <remarks>
    /// The human-readable name of the service (e.g., "Normaize API", "Database Service").
    /// This helps identify which service is being monitored in multi-service environments.
    /// </remarks>
    [Required]
    [StringLength(100)]
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the service
    /// </summary>
    /// <remarks>
    /// The semantic version of the service (e.g., "1.0.0", "2.1.3").
    /// This helps with version tracking and deployment monitoring.
    /// </remarks>
    [Required]
    [StringLength(20)]
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the environment where the service is running
    /// </summary>
    /// <remarks>
    /// The deployment environment (e.g., "Development", "Staging", "Production").
    /// This helps with environment-specific monitoring and debugging.
    /// </remarks>
    [Required]
    [StringLength(50)]
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;
}