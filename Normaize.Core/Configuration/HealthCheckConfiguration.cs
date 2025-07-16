using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Configuration;

public class HealthCheckConfiguration
{
    public const string SectionName = "HealthCheck";

    [Range(1, 300, ErrorMessage = "Database timeout must be between 1 and 300 seconds")]
    public int DatabaseTimeoutSeconds { get; set; } = 30;

    [Range(1, 300, ErrorMessage = "Application timeout must be between 1 and 300 seconds")]
    public int ApplicationTimeoutSeconds { get; set; } = 10;

    [Required(ErrorMessage = "Component names are required")]
    public ComponentNames ComponentNames { get; set; } = new();

    public bool IncludeDetailedErrors { get; set; } = false;

    public bool SkipMigrationsCheck { get; set; } = false;

    public bool SkipDatabaseCheck { get; set; } = false;
}

public class ComponentNames
{
    public string Database { get; set; } = "database";
    public string Application { get; set; } = "application";
} 