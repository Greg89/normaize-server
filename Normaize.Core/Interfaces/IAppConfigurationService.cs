namespace Normaize.Core.Interfaces;

public interface IAppConfigurationService
{
    /// <summary>
    /// Loads environment variables from .env file
    /// </summary>
    void LoadEnvironmentVariables();

    /// <summary>
    /// Gets the current environment name
    /// </summary>
    /// <returns>Environment name</returns>
    string GetEnvironment();

    /// <summary>
    /// Gets the Seq URL for logging
    /// </summary>
    /// <returns>Seq URL or null if not configured</returns>
    string? GetSeqUrl();

    /// <summary>
    /// Gets the Seq API key for logging
    /// </summary>
    /// <returns>Seq API key or null if not configured</returns>
    string? GetSeqApiKey();

    /// <summary>
    /// Gets the database configuration
    /// </summary>
    /// <returns>Database configuration</returns>
    DatabaseConfig GetDatabaseConfig();

    /// <summary>
    /// Checks if a database connection is configured
    /// </summary>
    /// <returns>True if database connection is configured</returns>
    bool HasDatabaseConnection();

    /// <summary>
    /// Checks if the current environment is production-like
    /// </summary>
    /// <returns>True if production-like environment</returns>
    bool IsProductionLike();

    /// <summary>
    /// Checks if the application is running in a container
    /// </summary>
    /// <returns>True if running in container</returns>
    bool IsContainerized();

    /// <summary>
    /// Gets the port number for the application
    /// </summary>
    /// <returns>Port number</returns>
    string GetPort();

    /// <summary>
    /// Gets the HTTPS port number
    /// </summary>
    /// <returns>HTTPS port number or null if not configured</returns>
    string? GetHttpsPort();
}

public record DatabaseConfig
{
    public string? Host { get; init; }
    public string? Database { get; init; }
    public string? User { get; init; }
    public string? Password { get; init; }
    public string Port { get; init; } = "3306";

    public string ToConnectionString() =>
        $"Server={Host};Database={Database};User={User};Password={Password};Port={Port};CharSet=utf8mb4;AllowLoadLocalInfile=true;Convert Zero Datetime=True;Allow Zero Datetime=True;";
}