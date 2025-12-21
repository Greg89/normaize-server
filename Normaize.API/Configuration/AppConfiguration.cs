using Normaize.Core.Constants;
using DotNetEnv;

namespace Normaize.API.Configuration;

public static class AppConfiguration
{
    public static void LoadEnvironmentVariables()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var projectRoot = currentDir;

        while (!File.Exists(Path.Combine(projectRoot, ".env")) && Directory.GetParent(projectRoot) != null)
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
        }

        var envPath = Path.Combine(projectRoot, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        else
        {
            Env.Load(); // Fallback to default behavior
        }
    }

    public static string GetEnvironment() =>
        Environment.GetEnvironmentVariable(SharedConstants.Environment.ASPNETCORE_ENVIRONMENT) ?? SharedConstants.Environment.DEVELOPMENT;

    public static string? GetSeqUrl() =>
        Environment.GetEnvironmentVariable("SEQ_URL");

    public static string? GetSeqApiKey() =>
        Environment.GetEnvironmentVariable("SEQ_API_KEY");

    public static DatabaseConfig GetDatabaseConfig()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ??
                   Environment.GetEnvironmentVariable("DB_HOST");
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ??
                       Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ??
                   Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ??
                       Environment.GetEnvironmentVariable("DB_PASSWORD");
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ??
                   Environment.GetEnvironmentVariable("DB_PORT") ??
                   "5432";

        return new DatabaseConfig
        {
            Host = host,
            Database = database,
            User = user,
            Password = password,
            Port = port
        };
    }

    public static bool HasDatabaseConnection()
    {
        var explicitConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
            Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return true;
        }

        var config = GetDatabaseConfig();
        return !string.IsNullOrEmpty(config.Host) &&
               !string.IsNullOrEmpty(config.Database) &&
               !string.IsNullOrEmpty(config.User) &&
               !string.IsNullOrEmpty(config.Password);
    }

    public static bool IsProductionLike()
    {
        var environment = GetEnvironment();
        return environment.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
               environment.Equals("Staging", StringComparison.OrdinalIgnoreCase) ||
               environment.Equals("Beta", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsContainerized()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")) ||
               File.Exists("/.dockerenv") ||
               Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }

    public static string GetPort() =>
        Environment.GetEnvironmentVariable("PORT") ?? "5000";

    public static string? GetHttpsPort() =>
        Environment.GetEnvironmentVariable("HTTPS_PORT");
}

public record DatabaseConfig
{
    public string? Host { get; init; }
    public string? Database { get; init; }
    public string? User { get; init; }
    public string? Password { get; init; }
    public string Port { get; init; } = "5432";

    public string ToConnectionString() =>
        $"Host={Host};Port={Port};Database={Database};Username={User};Password={Password};";
}