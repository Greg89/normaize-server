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
        Environment.GetEnvironmentVariable(AppConstants.Environment.ASPNETCORE_ENVIRONMENT) ?? AppConstants.Environment.DEVELOPMENT;

    public static string? GetSeqUrl() => 
        Environment.GetEnvironmentVariable("SEQ_URL");

    public static string? GetSeqApiKey() => 
        Environment.GetEnvironmentVariable("SEQ_API_KEY");

    public static DatabaseConfig GetDatabaseConfig()
    {
        var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST") ?? 
                        Environment.GetEnvironmentVariable("MYSQL_HOST") ?? 
                        Environment.GetEnvironmentVariable("DB_HOST");
        var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? 
                           Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? 
                           Environment.GetEnvironmentVariable("DB_NAME");
        var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER") ?? 
                       Environment.GetEnvironmentVariable("MYSQL_USER") ?? 
                       Environment.GetEnvironmentVariable("DB_USER");
        var mysqlPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD") ?? 
                           Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? 
                           Environment.GetEnvironmentVariable("DB_PASSWORD");
        var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? 
                       Environment.GetEnvironmentVariable("MYSQL_PORT") ?? 
                       Environment.GetEnvironmentVariable("DB_PORT") ?? 
                       "3306";

        return new DatabaseConfig
        {
            Host = mysqlHost,
            Database = mysqlDatabase,
            User = mysqlUser,
            Password = mysqlPassword,
            Port = mysqlPort
        };
    }

    public static bool HasDatabaseConnection()
    {
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
    public string Port { get; init; } = "3306";

    public string ToConnectionString() =>
        $"Server={Host};Database={Database};User={User};Password={Password};Port={Port};CharSet=utf8mb4;AllowLoadLocalInfile=true;Convert Zero Datetime=True;Allow Zero Datetime=True;";
} 